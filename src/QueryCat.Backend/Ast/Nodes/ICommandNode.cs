namespace QueryCat.Backend.Ast.Nodes;

internal interface ICommandNode
{
    /// <summary>
    /// Command name.
    /// </summary>
    string CommandName { get; }
}
