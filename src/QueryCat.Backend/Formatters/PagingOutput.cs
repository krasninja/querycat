using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Render rows by pages.
/// </summary>
public class PagingOutput : IRowsOutput
{
    private readonly IRowsOutput _rowsOutput;
    private int _rowsCounter;

    /// <summary>
    /// Return specific amount of rows and stop for user input. -1 means no paging.
    /// </summary>
    public int PagingRowsCount { get; set; } = 10;

    public PagingOutput(IRowsOutput rowsOutput)
    {
        _rowsOutput = rowsOutput;
    }

    /// <inheritdoc />
    public void Open() => _rowsOutput.Open();

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
        _rowsOutput.SetContext(queryContext);
    }

    /// <inheritdoc />
    public void Close() => _rowsOutput.Close();

    /// <inheritdoc />
    public void Reset()
    {
        _rowsCounter = 0;
        _rowsOutput.Reset();
    }

    /// <inheritdoc />
    public void Write(Row row)
    {
        if (PagingRowsCount != -1
            && _rowsCounter++ >= PagingRowsCount
            && !Console.IsInputRedirected
            && !Console.IsOutputRedirected)
        {
            _rowsCounter = 0;
            Console.WriteLine("--More--");
            Console.ReadKey();
        }
        _rowsOutput.Write(row);
    }
}
