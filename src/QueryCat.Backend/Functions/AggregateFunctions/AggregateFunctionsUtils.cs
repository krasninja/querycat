using System.Runtime.CompilerServices;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Utilities for aggregate functions.
/// </summary>
internal static class AggregateFunctionsUtils
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="state">Initial state.</param>
    /// <param name="value">New value.</param>
    /// <param name="func"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static void ExecuteWithNullInitialState(ref VariantValue state, in VariantValue value,
        VariantValue.OperationBinaryDelegate func)
    {
        if (!value.IsNull)
        {
            if (state.IsNull)
            {
                state = value;
            }
            else
            {
                var newState = func.Invoke(in state, in value, out var errorCode);
                if (errorCode == ErrorCode.OK)
                {
                    state = newState;
                }
            }
        }
    }
}
