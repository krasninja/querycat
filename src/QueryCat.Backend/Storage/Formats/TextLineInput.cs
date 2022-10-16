using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage.Formats;

/// <summary>
/// Text line input.
/// </summary>
internal sealed class TextLineInput : StreamRowsInput
{
    private readonly Column[] _customColumns =
    {
        new("filename", DataType.String, "File path"), // Index 0.
    };

    public TextLineInput(Stream stream) : base(new StreamReader(stream), new DelimiterStreamReader.ReaderOptions())
    {
        if (StreamReader?.BaseStream is not FileStream)
        {
            _customColumns = Array.Empty<Column>();
        }
    }

    /// <inheritdoc />
    protected override int Analyze(ICursorRowsIterator iterator)
    {
        SetColumns(RowsIteratorUtils.ResolveColumnsTypes(iterator));
        Columns[^1].Name = "text";
        return base.Analyze(iterator);
    }

    /// <inheritdoc />
    protected override Column[] GetVirtualColumns() => _customColumns;

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
