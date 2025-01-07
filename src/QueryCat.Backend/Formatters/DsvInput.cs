using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Delimiter separated values (DSV) input.
/// </summary>
internal class DsvInput : StreamRowsInput
{
    private bool? _hasHeader;

    public DsvOptions Options { get; }

    public DsvInput(DsvOptions dsvOptions, string? key = null)
        : base(new StreamReader(dsvOptions.Stream), dsvOptions.InputOptions, key ?? string.Empty)
    {
        Options = dsvOptions;
        _hasHeader = Options.HasHeader;
    }

    /// <inheritdoc />
    protected override async Task AnalyzeAsync(CacheRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        var hasHeader = _hasHeader ?? await RowsIteratorUtils.DetermineIfHasHeaderAsync(iterator, cancellationToken: cancellationToken);
        _hasHeader = hasHeader;
        iterator.SeekToHead();

        if (hasHeader)
        {
            // Parse head columns names.
            await iterator.MoveNextAsync(cancellationToken);
            var columnNames = GetCurrentInputValues(iterator.Current);
            if (columnNames.Length < 1)
            {
                throw new IOSourceException(Resources.Errors.NoColumns);
            }
            var columns = GetInputColumns();
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i].Name = columnNames[i].AsString;
            }
        }

        await RowsIteratorUtils.ResolveColumnsTypesAsync(iterator, cancellationToken: cancellationToken);
        // Remove header row since it is not a data row.
        if (hasHeader)
        {
            iterator.RemoveRowAt(0);
        }
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await base.ResetAsync(cancellationToken);
        // If we have header row the first values row would have non-zero
        // position.
        if (_hasHeader == true && StreamReader.BaseStream.Position == 0)
        {
            await ReadNextAsync(cancellationToken);
        }
    }
}
