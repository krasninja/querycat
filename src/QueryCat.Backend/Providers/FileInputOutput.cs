using System.ComponentModel;
using System.IO.Compression;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Data provider that uses local files to read/write rows.
/// </summary>
internal static class FileInputOutput
{
    private static readonly string[] CompressFilesExtensions = { ".gz" };

    [Description("Read data from a file.")]
    [FunctionSignature("read_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue ReadFile(FunctionCallInfo args)
    {
        var path = args.GetAt(0).AsString;
        (path, var funcArgs) = QueryStringParser.ParseUri(path);

        var formatter = args.Count > 1
            ? args.GetAt(1).AsObject as IRowsFormatter
            : GetFormatter(path, args.ExecutionThread, funcArgs);
        var files = GetFileInputsByPath(path, args.ExecutionThread, formatter, funcArgs).ToList();
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
        var (path, funcArgs) = QueryStringParser.ParseUri(pathArgument.AsString);

        var formatter = args.GetAt(1).AsObject as IRowsFormatter;
        formatter ??= GetFormatter(path, args.ExecutionThread, funcArgs);
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

    private static IEnumerable<IRowsInput> GetFileInputsByPath(
        string path,
        IExecutionThread thread,
        IRowsFormatter? formatter = null,
        FunctionArguments? funcArgs = null)
    {
        foreach (var file in GetFilesByPath(path))
        {
            var fileFormatter = formatter ?? GetFormatter(file, thread, funcArgs);
            Stream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (CompressFilesExtensions.Contains(Path.GetExtension(file).ToLower()))
            {
                fileStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            yield return fileFormatter.OpenInput(fileStream, path);
        }
    }

    private static IEnumerable<string> GetFilesByPath(string path)
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

    private static IRowsFormatter GetFormatter(string path, IExecutionThread thread,
        FunctionArguments? funcArgs = null)
    {
        var extension = Path.GetExtension(path).ToLower();
        if (CompressFilesExtensions.Contains(extension))
        {
            extension = Path.GetExtension(path.Substring(0, path.Length - extension.Length)).ToLower();
        }
        var formatter = FormattersInfo.CreateFormatter(extension, thread, funcArgs);
        return formatter ?? new TextLineFormatter();
    }
}
