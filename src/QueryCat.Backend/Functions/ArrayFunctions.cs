using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Array functions.
/// </summary>
internal static class ArrayFunctions
{
    [SafeFunction]
    [Description("Converts an array to a string by concatenating elements using the delimiter. NULL elements are replaced with null_string.")]
    [FunctionSignature("array_to_string(arr: array, delimiter: string, null_string?: string := null): string")]
    public static VariantValue ArrayToStringWithNull(IExecutionThread thread)
    {
        // Stack layout: [0] = arr, [1] = delimiter, [2] = null_string.
        var arr = thread.Stack[0];
        if (arr.IsNull)
        {
            return VariantValue.Null;
        }

        var delimiter = thread.Stack[1].AsString;
        var nullString = !thread.Stack[2].IsNull ? thread.Stack[2].AsString : string.Empty;

        if (arr.Type != DataType.Array)
        {
            return VariantValue.Null;
        }

        var list = arr.AsArrayUnsafe;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(delimiter);
            }
            var item = list[i];
            sb.Append(item.IsNull ? nullString : item.ToString(Application.Culture));
        }
        return new VariantValue(sb.ToString());
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(ArrayToStringWithNull);
    }
}
