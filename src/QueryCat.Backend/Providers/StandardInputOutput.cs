using System.ComponentModel;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Standard IO streams data providers.
/// </summary>
// ReSharper disable once InconsistentNaming
public static class StandardInputOutput
{
    private static Stream? outputStream;
    private static Stream? inputStream;
    private static readonly object ObjLock = new();

    [Description("Write data to the system standard output.")]
    [FunctionSignature("stdout(formatter?: object<IRowsFormatter>, page_size: integer = 10): object<IRowsOutput>")]
    public static VariantValue Stdout(FunctionCallInfo args)
    {
        var formatter = args.GetAt(0).AsObject as IRowsFormatter;
        var pageSize = (int)args.GetAt(1).AsInteger;

        var stream = GetConsoleOutput();
        formatter ??= new TextTableFormatter();
        var output = formatter.OpenOutput(stream);
        var pagingOutput = new PagingOutput(output)
        {
            PagingRowsCount = pageSize
        };
        pagingOutput.PagingRowsCount = pageSize;

        return VariantValue.CreateFromObject(pagingOutput);
    }

    [Description("Read data from the system standard input.")]
    [FunctionSignature("stdin(skip_lines: integer = 0, formatter?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue Stdin(FunctionCallInfo args)
    {
        var skipLines = args.GetAt(0).AsInteger;
        var formatter = args.GetAt(1).AsObject as IRowsFormatter;
        var stream = CreateConsoleInput();

        for (var i = 0; i < skipLines; i++)
        {
            ConsoleUtils.ReadToEndOfLine(stream);
        }

        formatter ??= new TextTableFormatter();
        var input = formatter.OpenInput(stream);
        return VariantValue.CreateFromObject(input);
    }

    public static Stream GetConsoleOutput()
    {
        lock (ObjLock)
        {
            return outputStream ??= Console.OpenStandardOutput();
        }
    }

    internal static Stream CreateConsoleInput()
    {
        lock (ObjLock)
        {
            return inputStream ??= Console.OpenStandardInput();
        }
    }
}
