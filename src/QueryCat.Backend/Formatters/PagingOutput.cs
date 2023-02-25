using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Render rows by pages.
/// </summary>
public class PagingOutput : IRowsOutput
{
    private const string ContinueWord = "--More--";

    private static readonly string ClearText = new('\r', ContinueWord.Length);

    private readonly IRowsOutput _rowsOutput;
    private int _rowsCounter;
    private QueryContext? _queryContext;

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
        _queryContext = queryContext;
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
        _rowsOutput.Write(row);
        if (PagingRowsCount != -1
            && ++_rowsCounter >= PagingRowsCount
            && !Console.IsInputRedirected
            && !Console.IsOutputRedirected)
        {
            _rowsCounter = 0;
            Console.Write(ContinueWord);
            var consoleKey = Console.ReadKey();
            Console.Write(ClearText);

            if (consoleKey.Key == ConsoleKey.A)
            {
                PagingRowsCount = -1;
            }
            else if (consoleKey.Key == ConsoleKey.Q && _queryContext != null)
            {
                _queryContext.ExecutionThread.CancellationTokenSource.Cancel();
            }
        }
    }
}
