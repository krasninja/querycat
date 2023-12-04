namespace QueryCat.Backend.Ast;

/// <summary>
/// Base abstract AST node class. Every AST node contains short code, child nodes and
/// set of attributes. Attributes can be used to store additional data while processing.
/// </summary>
internal abstract class AstNode : IAstNode
{
    private static int nextId = 1;

    /// <summary>
    /// Node identifier. It is kept when node is cloned.
    /// </summary>
    public int Id { get; private set; } = nextId++;

    private readonly SortedList<string, object?> _attributes = new();

    /// <inheritdoc />
    public abstract object Clone();

    /// <summary>
    /// Copy all attributes, identifier and position to destination node.
    /// </summary>
    /// <param name="toNode">Destination node.</param>
    protected void CopyTo(AstNode toNode)
    {
        toNode.Id = Id;
        foreach (var dictionaryEntry in _attributes)
        {
            toNode._attributes.Add(dictionaryEntry.Key, dictionaryEntry.Value);
        }
#if DEBUG
        toNode._attributes.Add("__cloned", Id);
#endif
    }

    #region Attributes

    public object? GetAttribute(string key) => GetAttribute<object>(key);

    /// <inheritdoc />
    public T? GetAttribute<T>(string key)
    {
        if (_attributes.TryGetValue(key, out var obj))
        {
            if (obj is not T obj1)
            {
                throw new InvalidOperationException($"Attribute is not of type {typeof(T).Name}.");
            }
            return obj1;
        }
        return default;
    }

    /// <inheritdoc />
    public void SetAttribute(string key, object? value) => _attributes[key] = value;

    internal IDictionary<string, object?> GetAttributes()
    {
        var dict = new Dictionary<string, object?>();
        foreach (var dictionaryEntry in _attributes)
        {
            var key = dictionaryEntry.Key;
            dict[key] = dictionaryEntry.Value;
        }
        return dict;
    }

    #endregion

    /// <inheritdoc />
    public abstract string Code { get; }

    /// <inheritdoc />
    public virtual IEnumerable<IAstNode> GetChildren() => Enumerable.Empty<IAstNode>();

    /// <inheritdoc />
    public abstract void Accept(AstVisitor visitor);
}
