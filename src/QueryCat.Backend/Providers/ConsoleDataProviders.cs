using System.ComponentModel;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage.Formats;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Console data providers.
/// </summary>
public static class ConsoleDataProviders
{
    private static Stream? stream;
    private static readonly object ObjLock = new();

    [Description("Write data to the system console.")]
    [FunctionSignature("console(formatter?: object<IRowsFormatter>, page_size: integer = 10): object<IRowsOutput>")]
    public static VariantValue Console(FunctionCallInfo args)
    {
        var formatter = args.GetAt(0).AsObject as IRowsFormatter;
        var pageSize = (int)args.GetAt(1).AsInteger;
        var output = CreateConsole(formatter, pageSize);
        output.PagingRowsCount = pageSize;
        return VariantValue.CreateFromObject(output);
    }

    public static PagingOutput CreateConsole(IRowsFormatter? formatter = null, int pageSize = 10)
    {
        formatter ??= new TextTableFormatter();
        lock (ObjLock)
        {
            stream = System.Console.OpenStandardOutput();
            var output = formatter.OpenOutput(stream);
            return new PagingOutput(output)
            {
                PagingRowsCount = pageSize
            };
        }
    }
}
