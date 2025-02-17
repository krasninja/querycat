using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Inputs;

/// <summary>
/// Implements retry resilience strategy with constant delay interval for rows input.
/// </summary>
internal sealed class RetryRowsInput : RetryRowsSource, IRowsInput
{
    [SafeFunction]
    [Description("Implements retry resilience strategy with constant delay interval for rows input.")]
    [FunctionSignature("retry_input(input: object<IRowsInput>, max_attempts: integer := 3, retry_interval_secs: float := 5.0): object<IRowsInput>")]
    public static VariantValue RetryInput(IExecutionThread thread)
    {
        var input = thread.Stack[0].AsRequired<IRowsInput>();
        var maxAttempts = (int)(thread.Stack[1].AsInteger ?? 3);
        var retryIntervalSecs = thread.Stack[2].AsFloat ?? 5.0;
        return VariantValue.CreateFromObject(
            new RetryRowsInput(input, maxAttempts, TimeSpan.FromSeconds(retryIntervalSecs)));
    }

    private readonly IRowsInput _rowsInput;

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => _rowsInput.UniqueKey;

    public RetryRowsInput(IRowsInput rowsInput, int maxAttempts = 3, TimeSpan? retryInterval = null)
        : base(rowsInput, maxAttempts, retryInterval)
    {
        _rowsInput = rowsInput;
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value) => _rowsInput.ReadValue(columnIndex, out value);

    /// <inheritdoc />
    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
    {
        return RetryWrapperAsync(async ct => await _rowsInput.ReadNextAsync(ct), cancellationToken);
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent("Retry", _rowsInput);
    }
}
