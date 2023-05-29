using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The rows output that does nothing.
/// </summary>
public sealed class NullRowsOutput : IRowsOutput
{
    public static NullRowsOutput Instance { get; } = new();

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = EmptyQueryContext.Empty;

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
