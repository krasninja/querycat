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
    [FunctionSignature("json(jsonpath?: string, indent?: int): object<IRowsFormatter>")]
    [FunctionFormatters(".json", "application/json")]
    public static VariantValue Json(IExecutionThread thread)
    {
        var path = thread.Stack.GetAtOrDefault(0).AsString;
        var indent = thread.Stack.GetAtOrDefault(1).AsInteger;
        var formatter = new JsonFormatter(path, (int?)indent);
        return VariantValue.CreateFromObject(formatter);
    }

    private readonly string? _jsonPath;
    private readonly int? _indent;

    public JsonFormatter(string? jsonPath, int? indent)
    {
        _jsonPath = jsonPath;
        _indent = indent;
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null)
    {
        var stream = blob.GetStream();
        return new JsonInput(stream, jsonPath: _jsonPath, key: key);
    }

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob)
    {
        var stream = blob.GetStream();
        return new JsonOutput(stream, _indent);
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Json);
    }
}
