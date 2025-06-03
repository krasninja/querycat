using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Cli.Infrastructure;

/// <summary>
/// Render rows by pages.
/// </summary>
public class PagingOutput : IRowsOutput
{
    public const int NoLimit = -1;

    private static readonly string _clearText = new string(' ', Resources.Messages.MoreRows.Length) + '\r';

    private readonly IRowsOutput _rowsOutput;
    private readonly CancellationTokenSource? _cancellationTokenSource;
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

    public PagingOutput(IRowsOutput rowsOutput, int pagingRowsCount = 20, CancellationTokenSource? cancellationTokenSource = null)
    {
        _rowsOutput = rowsOutput;
        _cancellationTokenSource = cancellationTokenSource;
        PagingRowsCount = pagingRowsCount;
    }

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) => _rowsOutput.OpenAsync(cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => _rowsOutput.CloseAsync(cancellationToken);

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _rowsCounter = 0;
        return _rowsOutput.ResetAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        await _rowsOutput.WriteValuesAsync(values, cancellationToken);
        if (PagingRowsCount != NoLimit
            && Environment.UserInteractive
            && ++_rowsCounter >= PagingRowsCount
            && !Console.IsInputRedirected
            && !Console.IsOutputRedirected)
        {
            _rowsCounter = 0;
            Console.Write(Resources.Messages.MoreRows);
            var consoleKey = Console.ReadKey();
            Console.Write(_clearText);

            // Show all next content.
            if (consoleKey.Key == ConsoleKey.A)
            {
                PagingRowsCount = -1;
            }
            // Quit.
            else if (consoleKey.Key == ConsoleKey.Q && _cancellationTokenSource != null)
            {
                await _cancellationTokenSource.CancelAsync();
            }
        }

        return ErrorCode.OK;
    }
}
