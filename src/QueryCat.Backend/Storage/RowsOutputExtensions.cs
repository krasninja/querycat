using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Extensions for <see cref="IRowsOutput" />.
/// </summary>
public static class RowsOutputExtensions
{
    /// <summary>
    /// Write rows iterator into output.
    /// </summary>
    /// <param name="output">Rows output.</param>
    /// <param name="iterator">Rows iterator.</param>
    /// <param name="adjustColumnsLengths">Should update columns widths.</param>
    /// <param name="configStorage">Configuration storage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async ValueTask WriteAsync(
        this IRowsOutput output,
        IRowsIterator iterator,
        bool adjustColumnsLengths = false,
        IConfigStorage? configStorage = null,
        CancellationToken cancellationToken = default)
    {
        output.QueryContext = new RowsOutputQueryContext(iterator.Columns, configStorage ?? NullConfigStorage.Instance);
        await output.OpenAsync(cancellationToken);
        try
        {
            if (adjustColumnsLengths)
            {
                iterator = new AdjustColumnsLengthsIterator(iterator);
            }
            while (await iterator.MoveNextAsync(cancellationToken))
            {
                await output.WriteValuesAsync(iterator.Current.Values, cancellationToken);
            }
        }
        finally
        {
            await output.CloseAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Write rows iterator into output.
    /// </summary>
    /// <param name="output">Rows output.</param>
    /// <param name="input">Rows input.</param>
    /// <param name="adjustColumnsLengths">Should update columns widths.</param>
    /// <param name="configStorage">Configuration storage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async ValueTask WriteAsync(
        this IRowsOutput output,
        IRowsInput input,
        bool adjustColumnsLengths = false,
        IConfigStorage? configStorage = null,
        CancellationToken cancellationToken = default)
    {
        await input.OpenAsync(cancellationToken);
        await WriteAsync(
            output,
            new RowsInputIterator(input),
            adjustColumnsLengths,
            configStorage,
            cancellationToken: cancellationToken);
        await input.CloseAsync(cancellationToken);
    }
}
