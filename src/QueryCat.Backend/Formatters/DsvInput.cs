using QueryCat.Backend.Core.Data;
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
    protected override void Analyze(CacheRowsIterator iterator)
    {
        var hasHeader = _hasHeader ?? RowsIteratorUtils.DetermineIfHasHeader(iterator);
        _hasHeader = hasHeader;
        iterator.SeekToHead();

        if (hasHeader)
        {
            // Parse head columns names.
            iterator.MoveNext();
            var columnNames = GetCurrentInputValues(iterator.Current);
            if (columnNames.Length < 1)
            {
                throw new IOSourceException("There are no columns.");
            }
            var columns = GetInputColumns();
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i].Name = columnNames[i].AsString;
            }
        }

        RowsIteratorUtils.ResolveColumnsTypes(iterator);
        // Remove header row since it is not a data row.
        if (hasHeader)
        {
            iterator.RemoveRowAt(0);
        }
    }

    /// <inheritdoc />
    public override void Reset()
    {
        base.Reset();
        // If we have header row the first values row would have non-zero
        // position.
        if (_hasHeader == true && StreamReader.BaseStream.Position == 0)
        {
            ReadNext();
        }
    }
}
