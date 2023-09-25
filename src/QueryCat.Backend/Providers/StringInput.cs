using System.ComponentModel;
using System.Text;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Provider to create rows set from a string.
/// </summary>
internal static class StringInput
{
    [Description("Reads data from a string.")]
    [FunctionSignature("read_text([text]: string, fmt: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue ReadString(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var formatter = (IRowsFormatter)args.GetAt(1).AsObject!;

        var stringStream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return VariantValue.CreateFromObject(formatter.OpenInput(stringStream));
    }
}
