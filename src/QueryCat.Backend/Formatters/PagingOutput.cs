using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Render rows by pages.
/// </summary>
public class PagingOutput : IRowsOutput
{
    public const int NoLimit = -1;
    private const string ContinueWord = "--More--";

    private static readonly string ClearText = new('\r', ContinueWord.Length);

    private readonly IRowsOutput _rowsOutput;
    private readonly CancellationTokenSource? _cts;
    private int _rowsCounter;

    /// <summary>
    /// Return specific amount of rows and stop for user input. -1 means no paging.
    /// </summary>
    public int PagingRowsCount { get; set; }

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _rowsOutput.QueryContext;
        set => _rowsOutput.QueryContext = value;
    }

    /// <inheritdoc />
    public RowsOutputOptions Options { get; } = new()
    {
        RequiresColumnsLengthAdjust = true,
    };

    public PagingOutput(IRowsOutput rowsOutput, int pagingRowsCount = 20, CancellationTokenSource? cts = null)
    {
        _rowsOutput = rowsOutput;
        _cts = cts;
        PagingRowsCount = pagingRowsCount;
    }

    /// <inheritdoc />
    public void Open() => _rowsOutput.Open();

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
        if (PagingRowsCount != NoLimit
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
            else if (consoleKey.Key == ConsoleKey.Q)
            {
                _cts?.Cancel();
            }
        }
    }
}
