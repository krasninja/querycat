using System.ComponentModel;
using System.Text;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Provider to create rows set from a string.
/// </summary>
public static class StringDataProviders
{
    [Description("Read data from a string.")]
    [FunctionSignature("read_string(text: string, formatter: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue ReadString(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var formatter = (IRowsFormatter)args.GetAt(1).AsObject!;

        text = text.Replace("\\n", Environment.NewLine);

        var stringStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return VariantValue.CreateFromObject(formatter.OpenInput(stringStream));
    }
}
