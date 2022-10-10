using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage.Formats;

/// <summary>
/// Delimiter separated values (DSV) input.
/// </summary>
public sealed class DsvInput : StreamRowsInput
{
    private const char QuoteChar = '\"';

    /// <inheritdoc />
    protected override StreamReader? StreamReader { get; set; }

    private readonly Column[] _customColumns =
    {
        new("filename", DataType.String, "File path"), // Index 0.
    };

    private bool? _hasHeader;

    public DsvInput(Stream stream, char delimiter, bool? hasHeader = null) :
        base(new StreamRowsInputOptions
        {
            BufferSize = StreamRowsInputOptions.DefaultBufferSize,
            Delimiters = new[] { delimiter },
            QuoteChar = QuoteChar,
            UseQuoteChar = true
        })
    {
        StreamReader = new StreamReader(stream);
        _hasHeader = hasHeader;

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
        RowsFrame? cache = null;
        for (var i = 0; i < 10 && iterator.MoveNext(); i++)
        {
            if (cache == null)
            {
                cache = new RowsFrame(iterator.Columns);
            }
            cache.AddRow(iterator.Current);
        }
        if (cache == null)
        {
            return;
        }
        var analyzeRowsIterator = cache.GetIterator();

        var hasHeader = _hasHeader ?? RowsIteratorUtils.DetermineIfHasHeader(cache.GetIterator());
        _hasHeader = hasHeader;

        if (hasHeader)
        {
            if (!analyzeRowsIterator.MoveNext())
            {
                throw new IOSourceException("There is no header row.");
            }

            // Parse head columns names.
            var columnNames = analyzeRowsIterator.Current.AsArray().Select(c => c.AsString).ToArray();
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

        Columns = RowsIteratorUtils.ResolveColumnsTypes(analyzeRowsIterator).ToArray();
    }

    /// <inheritdoc />
    protected override void Prepare(IRowsIterator iterator)
    {
        if (_hasHeader == true)
        {
            ReadNext();
        }
        base.Prepare(iterator);
    }

    /// <inheritdoc />
    protected override Column[] GetCustomColumns() => _customColumns;

    /// <inheritdoc />
    protected override VariantValue GetCustomColumnValue(int rowIndex, int columnIndex)
    {
        if (columnIndex == 0 && StreamReader?.BaseStream is FileStream fileStream)
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
        return base.GetCustomColumnValue(rowIndex, columnIndex);
    }
}
