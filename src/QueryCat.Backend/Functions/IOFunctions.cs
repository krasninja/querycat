using System.ComponentModel;
using System.IO.Compression;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Functions;

internal static class IOFunctions
{
    private const string ContentTypeHeader = "Content-Type";

    [SafeFunction]
    [Description("Read data from a URI.")]
    [FunctionSignature("read(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static async ValueTask<VariantValue> ReadAsync(IExecutionThread thread, CancellationToken cancellationToken)
    {
        var uri = thread.Stack[0].AsString;

        var function = thread.FunctionsManager.ResolveUri(uri);
        if (function != null)
        {
            return await FunctionCaller.CallAsync(function.Delegate, thread, cancellationToken);
        }

        return await ReadFileAsync(thread, cancellationToken);
    }

    [Description("Write data to a URI.")]
    [FunctionSignature("write(uri: string, fmt?: object<IRowsFormatter>): object<IRowsOutput>")]
    public static ValueTask<VariantValue> WriteAsync(IExecutionThread thread, CancellationToken cancellationToken)
    {
        return WriteFileAsync(thread, cancellationToken);
    }

    [SafeFunction]
    [Description("Reads data from a string.")]
    [FunctionSignature("read_text(\"text\": string, fmt: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue ReadString(IExecutionThread thread)
    {
        var text = thread.Stack[0].AsString;
        var formatter = (IRowsFormatter)thread.Stack[1].AsObject!;

        var stringStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        var blobStream = new StreamBlobData(stringStream);
        return VariantValue.CreateFromObject(formatter.OpenInput(blobStream));
    }

    #region File

    private static readonly string[] _compressFilesExtensions = [".gz"];

    [SafeFunction]
    [Description("Read data from a file.")]
    [FunctionSignature("read_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static async ValueTask<VariantValue> ReadFileAsync(IExecutionThread thread, CancellationToken cancellationToken)
    {
        var path = thread.Stack[0].AsString;
        (path, var funcArgs) = Utils_ParseUri(path);

        var formatter = thread.Stack.FrameLength > 1
            ? thread.Stack[1].AsObject as IRowsFormatter
            : await File_GetFormatterAsync(path, thread, funcArgs, cancellationToken);
        var files = await File_GetFileInputsByPath(path, thread, formatter, funcArgs).ToListAsync(cancellationToken);
        if (!files.Any())
        {
            throw new QueryCatException(string.Format(Resources.Errors.PathNoFiles, path));
        }
        var input = files.Count == 1 ? files.First() : new CombineRowsInput(files);
        return VariantValue.CreateFromObject(input);
    }

    [Description("Write data to a file.")]
    [FunctionSignature("write_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsOutput>")]
    public static async ValueTask<VariantValue> WriteFileAsync(IExecutionThread thread, CancellationToken cancellationToken)
    {
        var pathArgument = thread.Stack[0];
        if (pathArgument.IsNull || string.IsNullOrEmpty(pathArgument.AsString))
        {
            throw new QueryCatException(Resources.Errors.PathNotDefined);
        }
        var (path, funcArgs) = Utils_ParseUri(pathArgument.AsString);

        var formatter = thread.Stack[1].AsObject as IRowsFormatter;
        formatter ??= await File_GetFormatterAsync(path, thread, funcArgs, cancellationToken);
        var fullDirectory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(fullDirectory) && !Directory.Exists(fullDirectory))
        {
            Directory.CreateDirectory(fullDirectory);
        }
        Stream file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        if (_compressFilesExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
        {
            file = new GZipStream(file, CompressionMode.Compress, leaveOpen: false);
        }
        var blobFile = new StreamBlobData(file);
        return VariantValue.CreateFromObject(formatter.OpenOutput(blobFile));
    }

    private static async IAsyncEnumerable<IRowsInput> File_GetFileInputsByPath(
        string path,
        IExecutionThread thread,
        IRowsFormatter? formatter = null,
        FunctionCallArguments? funcArgs = null)
    {
        foreach (var file in File_GetFilesByPath(path))
        {
            Stream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (_compressFilesExtensions.Contains(Path.GetExtension(file).ToLower()))
            {
                fileStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            var fileFormatter = formatter ?? await File_GetFormatterAsync(file, thread, funcArgs);
            var blobFileStream = new StreamBlobData(fileStream);
            yield return fileFormatter.OpenInput(blobFileStream, path);
        }
    }

    private static IEnumerable<string> File_GetFilesByPath(string path)
    {
        // Try parse file URI scheme.
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
        {
            path = uri.LocalPath;
        }

        // The case when we query from a single file.
        if (File.Exists(path))
        {
            yield return path;
            yield break;
        }

        var dir = Path.GetDirectoryName(path) ?? string.Empty;
        var options = SearchOption.TopDirectoryOnly;
        if (path.EndsWith("**") || path.EndsWith("**/") || dir.EndsWith("**"))
        {
            dir = dir.Replace("**", string.Empty);
            options = SearchOption.AllDirectories;
        }
        var pattern = Path.GetFileName(path);
        if (string.IsNullOrEmpty(dir))
        {
            dir = ".";
        }
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(dir, pattern, options);
        }
        catch (Exception e)
        {
            throw new QueryCatException(string.Format(Resources.Errors.CannotEnumerateFiles, e.Message));
        }
        foreach (var file in files.OrderBy(f => f))
        {
            yield return file;
        }
    }

    private static async ValueTask<IRowsFormatter> File_GetFormatterAsync(
        string path,
        IExecutionThread thread,
        FunctionCallArguments? funcArgs = null,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path).ToLower();
        if (_compressFilesExtensions.Contains(extension))
        {
            extension = Path.GetExtension(path.Substring(0, path.Length - extension.Length)).ToLower();
        }
        var formatter = await FormattersInfo.CreateFormatterAsync(extension, thread, funcArgs, cancellationToken);
        return formatter ?? new TextLineFormatter();
    }

    private sealed class ListDirectoryEntry
    {
        [Description("File or directory.")]
        public string Type { get; init; } = string.Empty;

        [Description("Name of the file or directory.")]
        public required string Name { get; init; }

        [Description("Full path of the file or directory.")]
        public string Path { get; init; } = string.Empty;

        [Description("Size of the file, in bytes.")]
        public long? Size { get; init; }

        [Description("Date and time at (UTC) which the file or directory has been created ")]
        public DateTime CreatedAt { get; init; }

        [Description("Date and time (UTC) at which the file or directory has been last accessed.")]
        public DateTime LastAccessedAt { get; init; }

        [Description("Date and time (UTC) at which the file or directory has been last modified.")]
        public DateTime LastWriteTime { get; init; }
    }

    private static IEnumerable<ListDirectoryEntry> ListDirectoryInternal(string path)
    {
        var dirInfo = new DirectoryInfo(path);

        if (dirInfo.Parent != null)
        {
            yield return new ListDirectoryEntry
            {
                Type = "d",
                Name = "../",
                Path = dirInfo.FullName,
                CreatedAt = dirInfo.CreationTimeUtc,
                LastWriteTime = dirInfo.LastWriteTimeUtc,
                LastAccessedAt = dirInfo.LastAccessTimeUtc,
            };
        }

        foreach (var dir in dirInfo.EnumerateDirectories().OrderBy(d => d.Name))
        {
            yield return new ListDirectoryEntry
            {
                Type = "d",
                Name = dir.Name + '/',
                Path = dir.FullName + '/',
                CreatedAt = dir.CreationTimeUtc,
                LastWriteTime = dir.LastWriteTimeUtc,
                LastAccessedAt = dir.LastAccessTimeUtc,
            };
        }

        foreach (var file in dirInfo.EnumerateFiles().OrderBy(f => f.Name))
        {
            yield return new ListDirectoryEntry
            {
                Type = "f",
                Name = file.Name,
                Size = file.Length,
                Path = file.FullName,
                CreatedAt = file.CreationTimeUtc,
                LastWriteTime = file.LastWriteTimeUtc,
                LastAccessedAt = file.LastAccessTimeUtc,
            };
        }
    }

    [SafeFunction]
    [Description("List directory content (files and sub-directories).")]
    [FunctionSignature("ls_dir(path: string): object<IRowsInput>")]
    public static VariantValue ListDirectory(IExecutionThread thread)
    {
        var path = thread.Stack.Pop().AsString;
        path = ResolveHomeDirectory(path);

        var items = ListDirectoryInternal(path);
        var input = EnumerableRowsInput<ListDirectoryEntry>.FromSource(items,
            builder => builder
                .AddProperty("type", f => f.Type)
                .AddProperty("name", f => f.Name)
                .AddProperty("path", f => f.Path)
                .AddProperty("size", f => f.Size)
                .AddProperty("creation_time", f => f.CreatedAt)
                .AddProperty("last_access_time", f => f.LastAccessedAt)
                .AddProperty("last_write_time", f => f.LastWriteTime));
        return VariantValue.CreateFromObject(input);
    }

    internal static string ResolveHomeDirectory(string dir)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (dir == "~")
        {
            return homeDirectory;
        }
        if (dir.Length > 1 && dir[0] == '~' && (dir[1] == '\\' || dir[1] == '/'))
        {
            return Path.Combine(
                homeDirectory,
                dir.Substring(2, dir.Length - 2));
        }
        return dir;
    }

    #endregion

    #region Curl

    private static readonly HttpClient _httpClient = new();

    [SafeFunction]
    [Description("Read the HTTP resource.")]
    [FunctionSignature("curl(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static async ValueTask<VariantValue> CurlAsync(IExecutionThread thread, CancellationToken cancellationToken)
    {
        var uriArgument = thread.Stack[0].AsString;
        var formatter = thread.Stack[1].AsObject as IRowsFormatter;

        (uriArgument, var funcArgs) = Utils_ParseUri(uriArgument);

        if (!Uri.TryCreate(uriArgument, UriKind.Absolute, out var uri))
        {
            throw new QueryCatException(Resources.Errors.InvalidUri);
        }
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var response = await _httpClient.SendAsync(request, cancellationToken);

        // Try get formatter by HTTP response content type.
        if (formatter == null)
        {
            if (response.Headers.TryGetValues(ContentTypeHeader, out var contentTypes))
            {
                var contentType = contentTypes.Last();
                formatter = await FormattersInfo.CreateFormatterAsync(contentType, thread, funcArgs, cancellationToken);
            }
        }
        // Try get formatter by extension from URI.
        if (formatter == null)
        {
            var absolutePath = (request.RequestUri ?? uri).AbsolutePath;
            var extension = Path.GetExtension(absolutePath).ToLower();
            if (!string.IsNullOrEmpty(extension))
            {
                formatter = await FormattersInfo.CreateFormatterAsync(extension, thread, funcArgs, cancellationToken);
            }
        }
        formatter ??= new TextLineFormatter();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var blobStream = new StreamBlobData(stream);
        return VariantValue.CreateFromObject(formatter.OpenInput(blobStream));
    }

    #endregion

    #region Utils

    public const string QueryDelimiter = "??";

    /// <summary>
    /// Split uri string to URI and arguments. For example: /tmp/1.json&amp;&amp;q=123 => /tmp/1.json, q=123.
    /// </summary>
    /// <param name="uri">URI string.</param>
    /// <returns>URI and arguments.</returns>
    private static (string Uri, FunctionCallArguments Args) Utils_ParseUri(string uri)
    {
        var delimiterIndex = uri.IndexOf(QueryDelimiter, StringComparison.Ordinal);
        if (delimiterIndex == -1)
        {
            return (
                ResolveHomeDirectory(uri),
                new FunctionCallArguments());
        }
        else
        {
            return (
                ResolveHomeDirectory(uri.Substring(0, delimiterIndex)),
                FromQueryString(uri.Substring(delimiterIndex + QueryDelimiter.Length))
            );
        }
    }

    /// <summary>
    /// Create from query string. Example string: arg1=10&amp;Name=John.
    /// </summary>
    /// <param name="query">Query.</param>
    /// <returns>Instance of <see cref="FunctionCallArguments" />.</returns>
    private static FunctionCallArguments FromQueryString(string query)
    {
        var args = StringUtils.GetFieldsFromLine(query, delimiter: '&');
        var fa = new FunctionCallArguments();
        if (args.Length == 1 && args[0].IndexOf('=') == -1)
        {
            fa.Add(CreateValueFromString(args[0]));
        }
        else
        {
            foreach (var arg in args)
            {
                var delimiterIndex = arg.IndexOf('=');
                if (delimiterIndex == -1)
                {
                    continue;
                }
                var name = arg.Substring(0, delimiterIndex);
                var value = CreateValueFromString(arg.Substring(delimiterIndex + 1));
                fa.Add(name, value);
            }
        }
        return fa;
    }

    private static VariantValue CreateValueFromString(string str)
    {
        var type = DataTypeUtils.DetermineTypeByValue(str);
        if (type == DataType.String)
        {
            var stringValue = StringUtils.Unquote(str);
            stringValue = StringUtils.Unquote(stringValue, quoteChar: "'");
            return new VariantValue(StringUtils.Unescape(stringValue.ToString()));
        }
        if (VariantValue.TryCreateFromString(str, type, out var value))
        {
            return value;
        }
        throw new InvalidOperationException(string.Format(Resources.Errors.CannotParseValue, str));
    }

    #endregion

    #region Stdio

    [SafeFunction]
    [Description("Write data to the system standard output.")]
    [FunctionSignature("stdout(fmt?: object<IRowsFormatter>): object<IRowsOutput>")]
    public static VariantValue Stdout(IExecutionThread thread)
    {
        IRowsFormatter? formatter = null;
        if (thread.Stack.FrameLength > 0)
        {
            formatter = thread.Stack.Pop().AsObject as IRowsFormatter;
        }
        if (formatter == null)
        {
            formatter = new TextTableFormatter();
        }

        var stream = Stdio.GetConsoleOutput();
        var blobFile = new StreamBlobData(stream);
        var output = formatter.OpenOutput(blobFile);

        return VariantValue.CreateFromObject(output);
    }

    [SafeFunction]
    [Description("Read data from the system standard input.")]
    [FunctionSignature("stdin(skip_lines: integer = 0, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue Stdin(IExecutionThread thread)
    {
        var skipLines = thread.Stack[0].AsInteger;
        var formatter = thread.Stack[1].AsObject as IRowsFormatter;
        var stream = Stdio.GetConsoleInput();

        for (var i = 0; i < skipLines; i++)
        {
            ReadToEndOfLine(stream);
        }

        formatter ??= new TextTableFormatter();
        var blobStream = new StreamBlobData(stream);
        var input = formatter.OpenInput(blobStream);
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

        functionsManager.RegisterFunction(ReadAsync);
        functionsManager.RegisterFunction(WriteAsync);
        functionsManager.RegisterFunction(ListDirectory);

        functionsManager.RegisterFunction(ReadFileAsync);
        functionsManager.RegisterFunction(WriteFileAsync);

        functionsManager.RegisterFunction(ReadString);

        functionsManager.RegisterFunction(CurlAsync);
    }
}
