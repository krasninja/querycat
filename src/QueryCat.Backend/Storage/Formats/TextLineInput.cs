using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage.Formats;

/// <summary>
/// Text line input.
/// </summary>
public sealed class TextLineInput : StreamRowsInput
{
    /// <inheritdoc />
    protected override StreamReader? StreamReader { get; set; }

    private readonly Column[] _customColumns =
    {
        new("filename", DataType.String, "File path"), // Index 0.
    };

    public TextLineInput(Stream stream) : base(new StreamRowsInputOptions
    {
        BufferSize = StreamRowsInputOptions.DefaultBufferSize,
        UseQuoteChar = false
    })
    {
        StreamReader = new StreamReader(stream);

        if (StreamReader?.BaseStream is not FileStream)
        {
            _customColumns = Array.Empty<Column>();
        }
    }

    #region Header

    #endregion

    /// <inheritdoc />
    protected override void Analyze(IRowsIterator iterator)
    {
        Columns = RowsIteratorUtils.ResolveColumnsTypes(iterator).ToArray();
        Columns[^1].Name = "text";
    }

    /// <inheritdoc />
    protected override Column[] GetCustomColumns() => _customColumns;

    /// <inheritdoc />
    protected override VariantValue GetCustomColumnValue(int rowIndex, int columnIndex)
    {
        if (columnIndex == 0 && StreamReader?.BaseStream is FileStream fileStream)
        {
            return new VariantValue(fileStream.Name);
        }
        return base.GetCustomColumnValue(rowIndex, columnIndex);
    }
}
