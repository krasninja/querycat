using QueryCat.Backend.Commands.Select;

namespace QueryCat.Backend.Ast;

/// <summary>
/// AST constants.
/// </summary>
internal static class AstAttributeKeys
{
    /// <summary>
    /// Node result type.
    /// </summary>
    public const string TypeKey = "type_key";

    /// <summary>
    /// Associated function (of type <see cref="QueryCat.Backend.Core.Functions.IFunction" />).
    /// </summary>
    public const string FunctionKey = "function_key";

    /// <summary>
    /// Function call info (of type <see cref="QueryCat.Backend.Commands.FuncUnitCallInfo" />).
    /// </summary>
    public const string ArgumentsKey = "args_key";

    /// <summary>
    /// Target aggregate column.
    /// </summary>
    public const string InputAggregateIndexKey = "aggregate_index_key";

    /// <summary>
    /// Aggregate function data.
    /// </summary>
    public const string AggregateFunctionKey = "aggregate_function_key";

    /// <summary>
    /// Command statement evaluated result.
    /// </summary>
    public const string ResultKey = "result_key";

    /// <summary>
    /// Select command context. See <see cref="SelectCommandContext" />.
    /// </summary>
    public const string ContextKey = "context_key";

    /// <summary>
    /// Select input context. See <see cref="SelectInputQueryContext" />.
    /// </summary>
    public const string RowsInputContextKey = "input_context_key";

    /// <summary>
    /// Correspond identifier rows input column.
    /// </summary>
    public const string InputColumnKey = "input_column";
}
