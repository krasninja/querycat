using System.Collections.Immutable;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Ast;

/// <summary>
/// Base abstract AST node class. Every AST node contains short code, child nodes and
/// set of attributes. Attributes can be used to store additional data while processing.
/// </summary>
internal abstract class AstNode : IAstNode
{
    private const string ClonedKey = "__cloned";

    private static int _nextId = 1;

    /// <summary>
    /// Node identifier. It is kept when node is cloned.
    /// </summary>
    public int Id { get; } = _nextId++;

    private SmallDictionary<string, object?>? _attributes;

    /// <inheritdoc />
    public abstract object Clone();

    /// <summary>
    /// Copy all attributes, identifier and position to destination node.
    /// </summary>
    /// <param name="toNode">Destination node.</param>
    protected void CopyTo(AstNode toNode)
    {
        if (_attributes == null)
        {
            return;
        }

        toNode._attributes ??= new();
        foreach (var dictionaryEntry in _attributes)
        {
#if DEBUG
            if (dictionaryEntry.Key == ClonedKey)
            {
                continue;
            }
#endif
            toNode._attributes.Add(dictionaryEntry.Key, dictionaryEntry.Value);
        }
#if DEBUG
        toNode._attributes[ClonedKey] = Id;
#endif
    }

    #region Attributes

    public object? GetAttribute(string key) => GetAttribute<object>(key);

    /// <inheritdoc />
    public T? GetAttribute<T>(string key)
    {
        _attributes ??= new();
        if (_attributes.TryGetValue(key, out var obj))
        {
            if (obj is not T obj1)
            {
                throw new InvalidOperationException(
                    string.Format(Resources.Errors.AttributeIsNotOfType, typeof(T).Name));
            }
            return obj1;
        }
        return default;
    }

    /// <inheritdoc />
    public void SetAttribute(string key, object? value)
    {
        if (value == null)
        {
            return;
        }
        _attributes ??= new();
        _attributes[key] = value;
    }

    internal IReadOnlyDictionary<string, object?> GetAttributes()
    {
        if (_attributes == null)
        {
            return ImmutableDictionary<string, object?>.Empty;
        }

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
    public virtual IEnumerable<IAstNode> GetChildren() => [];

    /// <inheritdoc />
    public abstract ValueTask AcceptAsync(AstVisitor visitor, CancellationToken cancellationToken);
}
