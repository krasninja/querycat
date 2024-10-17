using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// The data needed to process aggregate functions.
/// </summary>
/// <param name="ReturnType">Aggregate return type.</param>
/// <param name="AggregateFunction">Instance of aggregate function.</param>
/// <param name="ValueGenerator">Function delegate. It ia mainly used to run and fill arguments before
/// aggregate function call.</param>
/// <param name="Node">Related AST node.</param>
/// <param name="Name">Target column name.</param>
internal sealed record AggregateTarget(
    DataType ReturnType,
    IAggregateFunction AggregateFunction,
    IFuncUnit ValueGenerator,
    FunctionCallNode Node,
    string Name);
