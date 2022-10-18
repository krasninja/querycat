using System.ComponentModel;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Data provider that uses local files to read/write rows.
/// </summary>
public static class FileDataProviders
{
    [Description("Read data from a file.")]
    [FunctionSignature("read_file(path: string, formatter?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue ReadFile(FunctionCallInfo args)
    {
        var path = args.GetAt(0);
        var formatter = args.GetAt(1).AsObject as IRowsFormatter;
        formatter ??= GetFormatter(path);
        var files = GetFileInputsByPath(path, formatter).ToList();
        if (!files.Any())
        {
            throw new QueryCatException($"No files match '{path}'.");
        }
        var input = new CombineRowsInput(files);
        return VariantValue.CreateFromObject(input);
    }

    [Description("Write data to a file.")]
    [FunctionSignature("write_file(path: string, formatter?: object<IRowsFormatter>): object<IRowsOutput>")]
    public static VariantValue WriteFile(FunctionCallInfo args)
    {
        var path = args.GetAt(0);
        var formatter = args.GetAt(1).AsObject as IRowsFormatter;
        formatter ??= GetFormatter(path);
        var file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        return VariantValue.CreateFromObject(formatter.OpenOutput(file));
    }

    private static IEnumerable<IRowsInput> GetFileInputsByPath(string path, IRowsFormatter? formatter = null)
    {
        foreach (var file in GetFilesByPath(path))
        {
            var fileFormatter = formatter ?? GetFormatter(file);
            var fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
        var files = Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly);
        foreach (var file in files.OrderBy(f => f))
        {
            yield return file;
        }
    }

    private static IRowsFormatter GetFormatter(string path)
    {
        var formatter = FormatUtils.GetFormatterByExtension(Path.GetExtension(path));
        return formatter ?? new TextLineFormatter();
    }
}
