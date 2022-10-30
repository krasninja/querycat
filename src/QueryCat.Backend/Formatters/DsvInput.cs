using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Delimiter separated values (DSV) input.
/// </summary>
internal sealed class DsvInput : StreamRowsInput
{
    private readonly Column[] _customColumns =
    {
        new("filename", DataType.String, "File path"), // Index 0.
    };

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
    protected override int Analyze(ICursorRowsIterator iterator)
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

        var newColumns = RowsIteratorUtils.ResolveColumnsTypes(iterator);
        SetColumns(newColumns);
        return hasHeader ? 0 : -1;
    }

    /// <inheritdoc />
    protected override Column[] GetVirtualColumns()
    {
        return Options.AddFileNameColumn && StreamReader.BaseStream is FileStream
            ? _customColumns
            : Array.Empty<Column>();
    }

    /// <inheritdoc />
    protected override VariantValue GetVirtualColumnValue(int rowIndex, int columnIndex)
    {
        if (columnIndex == 0 && StreamReader.BaseStream is FileStream fileStream)
        {
            return new VariantValue(fileStream.Name);
        }
        return base.GetVirtualColumnValue(rowIndex, columnIndex);
    }
}
