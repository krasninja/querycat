using System.ComponentModel;
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

    private readonly char _delimiter;
    private readonly bool? _hasHeader;
    private readonly bool _addFileNameColumn;

    [Description("CSV formatter.")]
    [FunctionSignature("csv(has_header?: boolean): object<IRowsFormatter>")]
    public static VariantValue Csv(FunctionCallInfo args)
    {
        var hasHeader = args.GetAt(0).AsBooleanNullable;
        var rowsSource = new DsvFormatter(',', hasHeader);
        return VariantValue.CreateFromObject(rowsSource);
    }

    [Description("TSV formatter.")]
    [FunctionSignature("tsv(has_header?: boolean): object")]
    public static VariantValue Tsv(FunctionCallInfo args)
    {
        var hasHeader = args.GetAt(0).AsBooleanNullable;
        var rowsSource = new DsvFormatter('\t', hasHeader);
        return VariantValue.CreateFromObject(rowsSource);
    }

    public DsvFormatter(char delimiter, bool? hasHeader = null, bool addFileNameColumn = true)
    {
        _delimiter = delimiter;
        _hasHeader = hasHeader;
        _addFileNameColumn = addFileNameColumn;
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
            AddFileNameColumn = _addFileNameColumn
        };
        if (_streamRowsInputOptions != null)
        {
            options.InputOptions = _streamRowsInputOptions;
        }
        else
        {
            options.InputOptions.DelimiterStreamReaderOptions.Delimiters = new[] { _delimiter };
        }
        return options;
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Csv);
        functionsManager.RegisterFunction(Tsv);
    }
}
