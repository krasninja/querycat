using System.Runtime.CompilerServices;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions.AggregateFunctions;

/// <summary>
/// Utilities for aggregate functions.
/// </summary>
internal static class AggregateFunctionsUtils
{
    /// <summary>
    /// The method applies following logic:
    /// 1. If next value is null - return.
    /// 2. If state is null - set it to the next value.
    /// 3. If state is not null - apply delegate.
    /// </summary>
    /// <param name="state">Initial state.</param>
    /// <param name="value">New value.</param>
    /// <param name="func">Delegate to invoke.</param>
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
