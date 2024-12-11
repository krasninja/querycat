using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// The rows output that does nothing.
/// </summary>
public sealed class NullRowsOutput : IRowsOutput
{
    public static NullRowsOutput Instance { get; } = new();

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    /// <inheritdoc />
    public RowsOutputOptions Options { get; } = new();

    private NullRowsOutput()
    {
    }

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public void Close()
    {
    }

    /// <inheritdoc />
    public void Reset()
    {
    }

    /// <inheritdoc />
    public ErrorCode WriteValues(VariantValue[] values) => ErrorCode.OK;
}
