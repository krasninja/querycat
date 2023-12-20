using System.ComponentModel;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Object functions.
/// </summary>
internal static class ObjectFunctions
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

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ObjectQuery);
    }
}
