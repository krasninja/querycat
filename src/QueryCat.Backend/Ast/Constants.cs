namespace QueryCat.Backend.Ast;

/// <summary>
/// AST constants.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// <see cref="Func{TResult}" /> delegate to get value.
    /// </summary>
    public const string FuncKey = "func_key";

    /// <summary>
    /// Node result type.
    /// </summary>
    public const string TypeKey = "type_key";

    /// <summary>
    /// Associated function (of type <see cref="QueryCat.Backend.Functions.Function" />).
    /// </summary>
    public const string FunctionKey = "function_key";

    /// <summary>
    /// Rows input of type <see cref="QueryCat.Backend.Storage.IRowsInput" />.
    /// </summary>
    public const string RowsInputKey = "rows_input_key";

    /// <summary>
    /// Function call info (of type <see cref="QueryCat.Backend.Functions.FunctionCallInfo" />).
    /// </summary>
    public const string ArgumentsKey = "args_key";

    /// <summary>
    /// Target aggregate column.
    /// </summary>
    public const string InputAggregateIndexKey = "aggregate_index_key";

    /// <summary>
    /// Set string representation data.
    /// </summary>
    public const string StringKey = "string_key";

    /// <summary>
    /// Aggregate function data.
    /// </summary>
    public const string AggregateFunctionKey = "aggregate_function_key";

    /// <summary>
    /// Command statement evaluated result.
    /// </summary>
    public const string ResultKey = "result_key";
}
