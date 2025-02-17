namespace QueryCat.Backend.Ast.Nodes.Select;

/// <summary>
/// Node that can have the alias name.
/// </summary>
internal interface ISelectAliasNode
{
    /// <summary>
    /// Query alias name.
    /// </summary>
    string Alias { get; }
}
