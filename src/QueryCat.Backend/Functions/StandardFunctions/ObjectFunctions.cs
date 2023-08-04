using System.ComponentModel;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// Object functions.
/// </summary>
public static class ObjectFunctions
{
    [Description("Extracts a scalar value from a POCO .NET object.")]
    [FunctionSignature("object_query(obj: void, query: string): string")]
    public static VariantValue ObjectQuery(FunctionCallInfo args)
    {
        var obj = args.GetAt(0).AsObject;
        var query = args.GetAt(1).AsString;
        var value = Utils.ObjectQuery.Query(obj, query);
        return VariantValue.CreateFromObject(value);
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ObjectQuery);
    }
}
