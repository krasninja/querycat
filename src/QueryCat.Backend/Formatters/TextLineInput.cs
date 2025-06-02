using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Text line input.
/// </summary>
internal sealed class TextLineInput : StreamRowsInput
{
    private readonly VirtualColumn[] _customColumns =
    [
        new("filename", DataType.String, "File path"), // Index 0.
    ];

    public TextLineInput(Stream stream, string? key = null) : base(stream, new StreamRowsInputOptions
    {
        DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
        {
            DetectDelimiter = false,
            Culture = Application.Culture
        }
    }, key ?? string.Empty)
    {
        if (StreamReader.BaseStream is not FileStream)
        {
            _customColumns = [];
        }
    }

    /// <inheritdoc />
    protected override async Task AnalyzeAsync(CacheRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        await RowsIteratorUtils.ResolveColumnsTypesAsync(iterator, cancellationToken: cancellationToken);
        Columns[^1].Name = "text";
    }

    /// <inheritdoc />
    protected override VirtualColumn[] GetVirtualColumns() => _customColumns;

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
