using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.IO;

internal static partial class Functions
{
    [Description("Write data to the system standard output.")]
    [FunctionSignature("stdout(fmt?: object<IRowsFormatter>, page_size: integer = 10): object<IRowsOutput>")]
    public static VariantValue Stdio_Stdout(FunctionCallInfo args)
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
    public static VariantValue Stdio_Stdin(FunctionCallInfo args)
    {
        var skipLines = args.GetAt(0).AsInteger;
        var formatter = args.GetAt(1).AsObject as IRowsFormatter;
        var stream = Stdio.CreateConsoleInput();

        for (var i = 0; i < skipLines; i++)
        {
            ConsoleUtils.ReadToEndOfLine(stream);
        }

        formatter ??= new TextTableFormatter();
        var input = formatter.OpenInput(stream);
        return VariantValue.CreateFromObject(input);
    }
}
