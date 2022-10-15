using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage.Formats;

/// <summary>
/// Delimiter separated values (DSV) input.
/// </summary>
internal sealed class DsvInput : StreamRowsInput
{
    private const char QuoteChar = '\"';

    private readonly Column[] _customColumns =
    {
        new("filename", DataType.String, "File path"), // Index 0.
    };

    private bool? _hasHeader;
    private readonly bool _addFileNameColumn;

    public DsvInput(Stream stream, char delimiter, bool? hasHeader = null, bool addFileNameColumn = true) :
        base(new StreamReader(stream), new DelimiterStreamReader.ReaderOptions
        {
            Delimiters = new[] { delimiter },
            QuoteChars = new[] { QuoteChar },
        })
    {
        _hasHeader = hasHeader;
        _addFileNameColumn = addFileNameColumn;

        if (StreamReader.BaseStream is not FileStream)
        {
            _customColumns = Array.Empty<Column>();
        }
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
            var columnNames = iterator.Current.AsArray().Select(c => c.AsString).ToArray();
            if (columnNames.Length < 1)
            {
                throw new IOSourceException("There are no columns.");
            }
            for (int i = 0; i < columnNames.Length; i++)
            {
                Columns[i].Name = columnNames[i];
            }
            for (var i = 0; i < _customColumns.Length; i++)
            {
                Columns[i].Name = _customColumns[i].Name;
            }
        }

        var newColumns = RowsIteratorUtils.ResolveColumnsTypes(iterator);
        SetColumns(newColumns);
        return hasHeader ? 0 : -1;
    }

    /// <inheritdoc />
    protected override Column[] GetVirtualColumns()
    {
        return _addFileNameColumn ? _customColumns : Array.Empty<Column>();
    }

    /// <inheritdoc />
    protected override VariantValue GetVirtualColumnValue(int rowIndex, int columnIndex)
    {
        if (columnIndex == 0 && StreamReader.BaseStream is FileStream fileStream)
        {
            if (rowIndex == 0 && _hasHeader == true)
            {
                return new VariantValue(_customColumns[columnIndex].Name);
            }
            else
            {
                return new VariantValue(fileStream.Name);
            }
        }
        return base.GetVirtualColumnValue(rowIndex, columnIndex);
    }
}
