using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Functions.AggregateFunctions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// The data needed to process aggregate functions.
/// </summary>
/// <param name="ReturnType">Aggregate return type.</param>
/// <param name="AggregateFunction">Instance of aggregate function.</param>
/// <param name="FunctionCallInfo">Related call info. It is used to fill arguments stack.</param>
/// <param name="ValueGenerator">Function delegate. It ia mainly used to run and fill arguments before
/// aggregate function call.</param>
/// <param name="Node">Related AST node.</param>
/// <param name="Name">Target column name.</param>
internal sealed record AggregateTarget(
    DataType ReturnType,
    IAggregateFunction AggregateFunction,
    FunctionCallInfo FunctionCallInfo,
    IFuncUnit ValueGenerator,
    FunctionCallNode Node,
    string Name);
