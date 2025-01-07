using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// JSON formatter.
/// </summary>
internal sealed class JsonFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("JSON formatter.")]
    [FunctionSignature("json(jsonpath?: string): object<IRowsFormatter>")]
    [FunctionFormatters(".json", "application/json")]
    public static VariantValue Json(IExecutionThread thread)
    {
        var rowsSource = new JsonFormatter(thread.Stack.GetAtOrDefault(0).AsString);
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

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Json);
    }
}
