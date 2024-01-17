namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Empty execution statistic implementation.
/// </summary>
public class NullExecutionStatistic : ExecutionStatistic
{
    public static ExecutionStatistic Instance { get; } = new NullExecutionStatistic();

    /// <inheritdoc />
    public override void AddError(in RowErrorInfo info)
    {
    }

    /// <inheritdoc />
    public override string Dump() => string.Empty;
}
