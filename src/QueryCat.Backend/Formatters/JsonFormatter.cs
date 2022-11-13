using System.ComponentModel;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// JSON formatter.
/// </summary>
internal sealed class JsonFormatter : IRowsFormatter
{
    [Description("JSON formatter.")]
    [FunctionSignature("json(): object<IRowsFormatter>")]
    public static VariantValue Json(FunctionCallInfo args)
    {
        var rowsSource = new JsonFormatter();
        return VariantValue.CreateFromObject(rowsSource);
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input)
        => new JsonInput(new StreamReader(input));

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
    {
        throw new NotImplementedException();
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Json);
    }
}
