using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Text line input.
/// </summary>
internal sealed class TextLineInput : StreamRowsInput
{
    private readonly Column[] _customColumns =
    {
        new("filename", DataType.String, "File path"), // Index 0.
    };

    public TextLineInput(Stream stream, string? key = null) : base(new StreamReader(stream), new StreamRowsInputOptions
    {
        DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
        {
            DetectDelimiter = false,
        }
    }, key ?? string.Empty)
    {
        if (StreamReader.BaseStream is not FileStream)
        {
            _customColumns = Array.Empty<Column>();
        }
    }

    /// <inheritdoc />
    protected override void Analyze(CacheRowsIterator iterator)
    {
        RowsIteratorUtils.ResolveColumnsTypes(iterator);
        Columns[^1].Name = "text";
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
