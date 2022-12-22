namespace QueryCat.Backend.Ast.Nodes.Select;

/// <summary>
/// Node that can have alias name.
/// </summary>
public interface ISelectAliasNode
{
    /// <summary>
    /// Query alias name.
    /// </summary>
    string Alias { get; }
}
