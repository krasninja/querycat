using System.ComponentModel;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage.Formats;

/// <summary>
/// Delimiter-separated values formatter.
/// </summary>
/// <remarks>
/// URL: https://en.wikipedia.org/wiki/Delimiter-separated_values.
/// </remarks>
public class DsvFormatter : IRowsFormatter
{
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

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input)
        => new DsvInput(input, _delimiter, _hasHeader, _addFileNameColumn);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new DsvOutput(output, _delimiter, _hasHeader ?? true);

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Csv);
        functionsManager.RegisterFunction(Tsv);
    }
}
