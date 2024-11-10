using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

internal sealed class NullOutput : RowsOutput
{
    [Description("NULL output.")]
    [FunctionSignature("write_null(): object<IRowsOutput>")]
    public static VariantValue Null(IExecutionThread thread)
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
