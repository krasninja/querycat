using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// JSON formatter.
/// </summary>
internal sealed class JsonFormatter : IRowsFormatter
{
    [Description("JSON formatter.")]
    [FunctionSignature("json(jsonpath?: string): object<IRowsFormatter>")]
    public static VariantValue Json(FunctionCallInfo args)
    {
        var rowsSource = new JsonFormatter(args.GetAtOrDefault(0).AsString);
        return VariantValue.CreateFromObject(rowsSource);
    }

    private readonly string? _jsonPath;

    public JsonFormatter(string? jsonPath)
    {
        _jsonPath = jsonPath;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null)
    {
        return new JsonInput(new StreamReader(input), jsonPath: _jsonPath, key: key);
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
        => new JsonOutput(output);

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Json);
    }
}
