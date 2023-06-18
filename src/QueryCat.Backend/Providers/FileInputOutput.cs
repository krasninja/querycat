using System.ComponentModel;
using System.IO.Compression;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Execution;
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
        var formatter = args.Count > 1 ? args.GetAt(1).AsObject as IRowsFormatter : GetFormatter(path, args.ExecutionThread);
        var files = GetFileInputsByPath(path, args.ExecutionThread, formatter).ToList();
        if (!files.Any())
        {
            throw new QueryCatException($"No files match '{path}'.");
        }
        var input = files.Count == 1 ? files.First() : new CombineRowsInput(files);
        input.QueryContext.InputInfo.InputArguments = new[] { path };
        return VariantValue.CreateFromObject(input);
    }

    [Description("Write data to a file.")]
    [FunctionSignature("write_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsOutput>")]
    public static VariantValue WriteFile(FunctionCallInfo args)
    {
        var path = args.GetAt(0);
        if (path.IsNull || string.IsNullOrEmpty(path.AsString))
        {
            throw new QueryCatException("Path is not defined.");
        }

        var formatter = args.GetAt(1).AsObject as IRowsFormatter;
        formatter ??= GetFormatter(path, args.ExecutionThread);
        Stream file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        if (CompressFilesExtensions.Contains(Path.GetExtension(path).ToLower()))
        {
            file = new GZipStream(file, CompressionMode.Compress, leaveOpen: false);
        }
        return VariantValue.CreateFromObject(formatter.OpenOutput(file));
    }

    private static IEnumerable<IRowsInput> GetFileInputsByPath(string path,
        ExecutionThread thread, IRowsFormatter? formatter = null)
    {
        foreach (var file in GetFilesByPath(path))
        {
            var fileFormatter = formatter ?? GetFormatter(file, thread);
            Stream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (CompressFilesExtensions.Contains(Path.GetExtension(file).ToLower()))
            {
                fileStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }
            yield return fileFormatter.OpenInput(fileStream);
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

    private static IRowsFormatter GetFormatter(string path, ExecutionThread thread)
    {
        var extension = Path.GetExtension(path).ToLower();
        if (CompressFilesExtensions.Contains(extension))
        {
            extension = Path.GetExtension(path.Substring(0, path.Length - extension.Length)).ToLower();
        }
        var formatter = FormattersInfo.CreateFormatter(extension, thread);
        return formatter ?? new TextLineFormatter();
    }
}
