using System.ComponentModel;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Delimiter-separated values formatter.
/// </summary>
/// <remarks>
/// URL: https://en.wikipedia.org/wiki/Delimiter-separated_values.
/// </remarks>
internal class DsvFormatter : IRowsFormatter
{
    private readonly StreamRowsInputOptions? _streamRowsInputOptions;

    private readonly char? _delimiter;
    private readonly bool? _hasHeader;
    private readonly bool _addFileNameColumn;
    private readonly bool _quoteStrings;

    [Description("CSV formatter.")]
    [FunctionSignature("csv(has_header?: boolean, delimiter?: string = null, quote_strings?: boolean = false): object<IRowsFormatter>")]
    public static VariantValue Csv(FunctionCallInfo args)
    {
        var hasHeader = args.GetAt(0).AsBooleanNullable;
        var delimiter = args.GetAt(1).AsString;
        var quoteStrings = args.GetAt(2).AsBoolean;
        if (delimiter.Length != 0 && delimiter.Length > 1)
        {
            throw new QueryCatException("Delimiter must be one character.");
        }

        var rowsSource = new DsvFormatter(
            delimiter: delimiter.Length == 1 ? delimiter[0] : null,
            hasHeader: hasHeader,
            quoteStrings: quoteStrings);
        return VariantValue.CreateFromObject(rowsSource);
    }

    [Description("TSV formatter.")]
    [FunctionSignature("tsv(has_header?: boolean, quote_strings?: boolean = false): object")]
    public static VariantValue Tsv(FunctionCallInfo args)
    {
        var hasHeader = args.GetAt(0).AsBooleanNullable;
        var quoteStrings = args.GetAt(1).AsBoolean;
        var rowsSource = new DsvFormatter('\t', hasHeader, quoteStrings: quoteStrings);
        return VariantValue.CreateFromObject(rowsSource);
    }

    public DsvFormatter(char? delimiter = null, bool? hasHeader = null, bool addFileNameColumn = true,
        bool quoteStrings = false)
    {
        _delimiter = delimiter;
        _hasHeader = hasHeader;
        _addFileNameColumn = addFileNameColumn;
        _quoteStrings = quoteStrings;
    }

    public DsvFormatter(StreamRowsInputOptions streamRowsInputOptions)
    {
        _streamRowsInputOptions = streamRowsInputOptions;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input)
        => new DsvInput(GetOptions(input));

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new DsvOutput(GetOptions(output));

    private DsvOptions GetOptions(Stream stream)
    {
        var options = new DsvOptions(stream)
        {
            HasHeader = _hasHeader,
            QuoteStrings = _quoteStrings,
        };
        if (_streamRowsInputOptions != null)
        {
            options.InputOptions = _streamRowsInputOptions;
        }
        else
        {
            if (_delimiter.HasValue)
            {
                options.InputOptions.DelimiterStreamReaderOptions.Delimiters = new[] { _delimiter.Value };
            }
            options.InputOptions.DelimiterStreamReaderOptions.PreferredDelimiter = ',';
            options.InputOptions.AddInputSourceColumn = _addFileNameColumn;
        }
        return options;
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Csv);
        functionsManager.RegisterFunction(Tsv);
    }
}
