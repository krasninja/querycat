using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The rows output that does nothing.
/// </summary>
public sealed class NullRowsOutput : IRowsOutput
{
    public static NullRowsOutput Instance { get; } = new();

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = EmptyQueryContext.Empty;

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
    public void Write(Row row)
    {
    }
}
