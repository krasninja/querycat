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

    public DsvInput(DsvOptions dsvOptions) : base(new StreamReader(dsvOptions.Stream), dsvOptions.InputOptions)
    {
        Options = dsvOptions;
        _hasHeader = Options.HasHeader;
    }

    #region Header

    #endregion

    /// <inheritdoc />
    protected override void Analyze(CacheRowsIterator iterator)
    {
        var hasHeader = _hasHeader ?? RowsIteratorUtils.DetermineIfHasHeader(iterator);
        _hasHeader = hasHeader;
        iterator.Seek(-1, CursorSeekOrigin.Begin);

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
}
