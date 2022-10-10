namespace QueryCat.Backend.Ast;

/// <summary>
/// AST node. Every AST node should be able to dump its state.
/// </summary>
public interface IAstNode : ICloneable
{
    /// <summary>
    /// Node id.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Short self descriptive code.
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Get children nodes.
    /// </summary>
    IEnumerable<IAstNode> GetChildren();

    /// <summary>
    /// The accept method for visitor pattern implementation.
    /// </summary>
    /// <param name="visitor">The visitor.</param>
    void Accept(AstVisitor visitor);

    /// <summary>
    /// Get attribute assigned to the node.
    /// </summary>
    /// <param name="key">Attribute key.</param>
    /// <typeparam name="T">Expected attribute type.</typeparam>
    /// <returns>Attribute value.</returns>
    T? GetAttribute<T>(string key);

    /// <summary>
    /// Set attribute value.
    /// </summary>
    /// <param name="key">Attribute key.</param>
    /// <param name="value">Value to assign.</param>
    void SetAttribute(string key, object? value);
}
