namespace QueryCat.Backend.Ast.Nodes.Select;

internal interface ISelectJoinedNode
{
    /// <summary>
    /// Additional join operation nodes.
    /// </summary>
    List<SelectTableJoinedNode> JoinedNodes { get; }
}
