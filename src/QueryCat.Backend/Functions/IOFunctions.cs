using System.ComponentModel;
using System.IO.Compression;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Functions;

internal static class IOFunctions
{
    private const string ContentTypeHeader = "Content-Type";

    [Description("Read data from a URI.")]
    [FunctionSignature("read(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue Read(FunctionCallInfo args)
    {
        var uri = args.GetAt(0).AsString;
        if (uri.StartsWith(@"http://", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith(@"https://", StringComparison.OrdinalIgnoreCase))
        {
            return Curl(args);
        }

        return ReadFile(args);
    }

    [Description("Write data to a URI.")]
    [FunctionSignature("write(uri: string, fmt?: object<IRowsFormatter>): object<IRowsOutput>")]
    public static VariantValue Write(FunctionCallInfo args)
    {
        return WriteFile(args);
    }

    [Description("Reads data from a string.")]
    [FunctionSignature("read_text([text]: string, fmt: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue ReadString(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var formatter = (IRowsFormatter)args.GetAt(1).AsObject!;

        var stringStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        return VariantValue.CreateFromObject(formatter.OpenInput(stringStream));
    }

    #region File

    private static readonly string[] CompressFilesExtensions = { ".gz" };

    [Description("Read data from a file.")]
    [FunctionSignature("read_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue ReadFile(FunctionCallInfo args)
    {
        var path = args.GetAt(0).AsString;
        (path, var funcArgs) = Utils_ParseUri(path);

        var formatter = args.Count > 1
            ? args.GetAt(1).AsObject as IRowsFormatter
            : File_GetFormatter(path, args.ExecutionThread, funcArgs);
        var files = File_GetFileInputsByPath(path, args.ExecutionThread, formatter, funcArgs).ToList();
        if (!files.Any())
        {
            throw new QueryCatException($"No files match '{path}'.");
        }
        var input = files.Count == 1 ? files.First() : new CombineRowsInput(files);
        return VariantValue.CreateFromObject(input);
    }

    [Description("Write data to a file.")]
    [FunctionSignature("write_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsOutput>")]
    public static VariantValue WriteFile(FunctionCallInfo args)
    {
        var pathArgument = args.GetAt(0);
        if (pathArgument.IsNull || string.IsNullOrEmpty(pathArgument.AsString))
        {
            throw new QueryCatException("Path is not defined.");
        }
        var (path, funcArgs) = Utils_ParseUri(pathArgument.AsString);

        var formatter = args.GetAt(1).AsObject as IRowsFormatter;
        formatter ??= File_GetFormatter(path, args.ExecutionThread, funcArgs);
        var fullDirectory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(fullDirectory) && !Directory.Exists(fullDirectory))
        {
            Directory.CreateDirectory(fullDirectory);
        }
        Stream file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        if (CompressFilesExtensions.Contains(Path.GetExtension(path).ToLower()))
        {
            file = new GZipStream(file, CompressionMode.Compress, leaveOpen: false);
        }
        return VariantValue.CreateFromObject(formatter.OpenOutput(file));
    }

    private static IEnumerable<IRowsInput> File_GetFileInputsByPath(
        string path,
        IExecutionThread thread,
        IRowsFormatter? formatter = null,
        FunctionCallArguments? funcArgs = null)
    {
        foreach (var file in File_GetFilesByPath(path))
        {
            var fileFormatter = formatter ?? File_GetFormatter(file, thread, funcArgs);
            Stream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (CompressFilesExtensions.Contains(Path.GetExtension(file).ToLower()))
            {
                fileStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            yield return fileFormatter.OpenInput(fileStream, path);
        }
    }

    private static IEnumerable<string> File_GetFilesByPath(string path)
    {
        // The case when we query from a single file.
        if (File.Exists(path))
        {
            yield return path;
            yield break;
        }

        var dir = Path.GetDirectoryName(path);
        var pattern = Path.GetFileName(path);
        if (string.IsNullOrEmpty(dir))
        {
            dir = ".";
        }
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly);
        }
        catch (Exception e)
        {
            throw new QueryCatException($"Cannot enumerate files: {e.Message}");
        }
        foreach (var file in files.OrderBy(f => f))
        {
            yield return file;
        }
    }

    private static IRowsFormatter File_GetFormatter(string path, IExecutionThread thread,
        FunctionCallArguments? funcArgs = null)
    {
        var extension = Path.GetExtension(path).ToLower();
        if (CompressFilesExtensions.Contains(extension))
        {
            extension = Path.GetExtension(path.Substring(0, path.Length - extension.Length)).ToLower();
        }
        var formatter = FormattersInfo.CreateFormatter(extension, thread, funcArgs);
        return formatter ?? new TextLineFormatter();
    }

    #endregion

    #region Curl

    private static readonly HttpClient HttpClient = new();

    [Description("Read the HTTP resource.")]
    [FunctionSignature("curl(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue Curl(FunctionCallInfo args)
    {
        var uriArgument = args.GetAt(0).AsString;
        var formatter = args.GetAt(1).AsObject as IRowsFormatter;

        (uriArgument, var funcArgs) = Utils_ParseUri(uriArgument);

        if (!Uri.TryCreate(uriArgument, UriKind.Absolute, out var uri))
        {
            throw new QueryCatException("Invalid URI.");
        }
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var response = HttpClient.Send(request);

        // Try get formatter by HTTP response content type.
        if (formatter == null)
        {
            if (response.Headers.TryGetValues(ContentTypeHeader, out var contentTypes))
            {
                var contentType = contentTypes.Last();
                formatter = FormattersInfo.CreateFormatter(contentType, args.ExecutionThread, funcArgs);
            }
        }
        // Try get formatter by extension from URI.
        if (formatter == null)
        {
            var absolutePath = (request.RequestUri ?? uri).AbsolutePath;
            var extension = Path.GetExtension(absolutePath).ToLower();
            if (!string.IsNullOrEmpty(extension))
            {
                formatter = FormattersInfo.CreateFormatter(extension, args.ExecutionThread, funcArgs);
            }
        }
        formatter ??= new TextLineFormatter();

        var stream = response.Content.ReadAsStream();
        return VariantValue.CreateFromObject(formatter.OpenInput(stream));
    }

    #endregion

    #region Utils

    public const string QueryDelimiter = "??";

    /// <summary>
    /// Split uri string to URI and arguments. For example: /tmp/1.json&amp;&amp;q=123 => /tmp/1.json, q=123.
    /// </summary>
    /// <param name="uri">URI string.</param>
    /// <returns>URI and arguments.</returns>
    public static (string Uri, FunctionCallArguments Args) Utils_ParseUri(string uri)
    {
        var delimiterIndex = uri.IndexOf(QueryDelimiter, StringComparison.Ordinal);
        if (delimiterIndex == -1)
        {
            return (uri, new FunctionCallArguments());
        }
        else
        {
            return (
                uri.Substring(0, delimiterIndex),
                FunctionCallArguments.FromQueryString(uri.Substring(delimiterIndex + QueryDelimiter.Length))
            );
        }
    }

    #endregion

    #region Stdio

    [Description("Write data to the system standard output.")]
    [FunctionSignature("stdout(fmt?: object<IRowsFormatter>, page_size: integer = 10): object<IRowsOutput>")]
    public static VariantValue Stdout(FunctionCallInfo args)
    {
        var formatter = args.GetAt(0).AsObject as IRowsFormatter;
        var pageSize = (int)args.GetAt(1).AsInteger;

        var stream = Stdio.GetConsoleOutput();
        formatter ??= new TextTableFormatter();
        var output = formatter.OpenOutput(stream);
        var pagingOutput = new PagingOutput(output, cts: args.ExecutionThread.CancellationTokenSource)
        {
            PagingRowsCount = pageSize,
        };
        pagingOutput.PagingRowsCount = pageSize;

        return VariantValue.CreateFromObject(pagingOutput);
    }

    [Description("Read data from the system standard input.")]
    [FunctionSignature("stdin(skip_lines: integer = 0, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue Stdin(FunctionCallInfo args)
    {
        var skipLines = args.GetAt(0).AsInteger;
        var formatter = args.GetAt(1).AsObject as IRowsFormatter;
        var stream = Stdio.CreateConsoleInput();

        for (var i = 0; i < skipLines; i++)
        {
            ReadToEndOfLine(stream);
        }

        formatter ??= new TextTableFormatter();
        var input = formatter.OpenInput(stream);
        return VariantValue.CreateFromObject(input);
    }

    /// <summary>
    /// Reads the standard input stream to the end of line.
    /// </summary>
    /// <param name="stream">Standard stream.</param>
    /// <returns><c>True</c> if end of line reached, or <c>false</c> if there is end of stream.</returns>
    private static bool ReadToEndOfLine(Stream stream)
    {
        var isPosix = !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows);

        var prevch = '\0';
        var arr = new byte[] { 0 };
        while (stream.Read(arr, 0, 1) > 0)
        {
            var ch = (char)arr[0];
            if ((isPosix && ch == '\n')
                || (!isPosix && prevch == '\r' && ch == '\n'))
            {
                return true;
            }
            prevch = ch;
        }
        return false;
    }

    #endregion

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Stdout);
        functionsManager.RegisterFunction(Stdin);

        functionsManager.RegisterFunction(Read);
        functionsManager.RegisterFunction(Write);

        functionsManager.RegisterFunction(ReadFile);
        functionsManager.RegisterFunction(WriteFile);

        functionsManager.RegisterFunction(ReadString);

        functionsManager.RegisterFunction(Curl);
    }
}
