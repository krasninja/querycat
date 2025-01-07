using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

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
    private readonly bool _skipEmptyLines;
    private readonly bool _delimiterCanRepeat;

    [SafeFunction]
    [Description("CSV formatter.")]
    [FunctionSignature("""
        csv(
            has_header?: boolean,
            delimiter?: string := null,
            quote_strings?: boolean := false,
            skip_empty_lines?: boolean := true,
            delimiter_can_repeat?: boolean := false)
                : object<IRowsFormatter>
        """)]
    [FunctionFormatters(".csv", "text/csv", "text/csv", "text/x-csv", "application/csv", "application/x-csv")]
    public static VariantValue Csv(IExecutionThread thread)
    {
        var hasHeader = thread.Stack[0].AsBooleanNullable;
        var delimiter = thread.Stack[1].AsString;
        var quoteStrings = thread.Stack[2].AsBoolean;
        var skipEmptyLines = thread.Stack[3].AsBoolean;
        var delimiterCanRepeat = thread.Stack[4].AsBoolean;
        if (delimiter.Length != 0 && delimiter.Length > 1)
        {
            throw new QueryCatException(Resources.Errors.DelimiterOneCharacter);
        }

        var rowsSource = new DsvFormatter(
            delimiter: delimiter.Length == 1 ? delimiter[0] : null,
            hasHeader: hasHeader,
            quoteStrings: quoteStrings,
            skipEmptyLines: skipEmptyLines,
            delimiterCanRepeat: delimiterCanRepeat);
        return VariantValue.CreateFromObject(rowsSource);
    }

    [Description("TSV formatter.")]
    [FunctionSignature("tsv(has_header?: boolean, quote_strings?: boolean = false): object")]
    public static VariantValue Tsv(IExecutionThread thread)
    {
        var hasHeader = thread.Stack[0].AsBooleanNullable;
        var quoteStrings = thread.Stack[1].AsBoolean;
        var rowsSource = new DsvFormatter('\t', hasHeader, quoteStrings: quoteStrings);
        return VariantValue.CreateFromObject(rowsSource);
    }

    public DsvFormatter(
        char? delimiter = null,
        bool? hasHeader = null,
        bool addFileNameColumn = true,
        bool quoteStrings = false,
        bool skipEmptyLines = true,
        bool delimiterCanRepeat = false)
    {
        _delimiter = delimiter;
        _hasHeader = hasHeader;
        _addFileNameColumn = addFileNameColumn;
        _quoteStrings = quoteStrings;
        _skipEmptyLines = skipEmptyLines;
        _delimiterCanRepeat = delimiterCanRepeat;
    }

    public DsvFormatter(StreamRowsInputOptions streamRowsInputOptions)
    {
        _streamRowsInputOptions = streamRowsInputOptions;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null)
        => new DsvInput(GetOptions(input), key);

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
                options.InputOptions.DelimiterStreamReaderOptions.DelimitersCanRepeat = _delimiterCanRepeat;
                options.InputOptions.DelimiterStreamReaderOptions.SkipEmptyLines = _skipEmptyLines;
            }
            options.InputOptions.DelimiterStreamReaderOptions.PreferredDelimiter = ',';
            options.InputOptions.AddInputSourceColumn = _addFileNameColumn;
        }
        return options;
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Csv);
        functionsManager.RegisterFunction(Tsv);
    }
}
