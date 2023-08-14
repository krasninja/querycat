using System.ComponentModel;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

internal sealed class NullOutput : RowsOutput
{
    [Description("NULL output.")]
    [FunctionSignature("write_null(): object<IRowsOutput>")]
    public static VariantValue Null(FunctionCallInfo args)
    {
        var rowsSource = new NullFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public override void Open()
    {
    }

    /// <inheritdoc />
    public override void Close()
    {
    }

    /// <inheritdoc />
    protected override void OnWrite(in VariantValue[] values)
    {
    }
}
