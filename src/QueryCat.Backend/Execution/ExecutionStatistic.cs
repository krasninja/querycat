using System.Text;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The class contains execution statistic (execution time, errors, etc).
/// </summary>
public sealed class ExecutionStatistic
{
    private readonly Dictionary<ErrorCode, long> _statistic = new();

    public TimeSpan ExecutionTime { get; set; }

    public long ProcessedCount { get; set; }

    /// <summary>
    /// Does it have errors.
    /// </summary>
    public bool HasErrors => _statistic.Any();

    public void IncrementErrorsCount(ErrorCode code)
    {
        if (code == ErrorCode.OK)
        {
            return;
        }
        _statistic.AddOrUpdate(code, _ => 0, (_, value) => ++value);
    }

    public void Clear()
    {
        ExecutionTime = TimeSpan.Zero;
        ProcessedCount = 0;
        _statistic.Clear();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Execution time: " + ExecutionTime);
        sb.AppendLine("Rows processed: " + ProcessedCount);
        if (HasErrors)
        {
            sb.AppendLine("Errors:");
            foreach (var item in _statistic)
            {
                sb.AppendLine($"- {item.Key}: {item.Value}");
            }
        }
        return sb.ToString();
    }
}
