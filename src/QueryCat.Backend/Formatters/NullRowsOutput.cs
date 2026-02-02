using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

internal sealed class NullRowsOutput : RowsOutput
{
    public static NullRowsOutput Instance { get; } = new();

    [Description("NULL output.")]
    [FunctionSignature("write_null(): object<IRowsOutput>")]
    public static VariantValue Null(IExecutionThread thread)
    {
        var rowsFormatter = new NullRowsFormatter();
        return VariantValue.CreateFromObject(rowsFormatter);
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override ValueTask<ErrorCode> OnWriteAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(ErrorCode.OK);
    }
}
