using QueryCat.Backend.Types;

namespace QueryCat.Backend.Ast.Nodes.Function;

/// <summary>
/// Function signature contains function name, argument and
/// return type.
/// </summary>
public sealed class FunctionSignatureNode : AstNode, IEquatable<FunctionSignatureNode>
{
    /// <summary>
    /// Function name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Function return type. VOID - no return.
    /// </summary>
    public DataType ReturnType { get; }

    /// <summary>
    /// Argument nodes.
    /// </summary>
    public FunctionSignatureArgumentNode[] ArgumentNodes { get; }

    /// <inheritdoc />
    public override string Code => "func_sig";

    /// <inheritdoc />
    public FunctionSignatureNode(
        string name,
        DataType returnType,
        IEnumerable<FunctionSignatureArgumentNode>? argumentNodes = null)
    {
        Name = name;
        ReturnType = returnType;
        ArgumentNodes = argumentNodes != null ? argumentNodes.ToArray()
            : Array.Empty<FunctionSignatureArgumentNode>();
    }

    public FunctionSignatureNode(FunctionSignatureNode node) :
        this(
            node.Name,
            node.ReturnType,
            node.ArgumentNodes.Select(an => (FunctionSignatureArgumentNode)an.Clone()))
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override IEnumerable<IAstNode> GetChildren() => ArgumentNodes;

    /// <inheritdoc />
    public override object Clone() => new FunctionSignatureNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString()
        => $"{Name}({string.Join(", ", ArgumentNodes.AsEnumerable())}): {ReturnType}";

    /// <inheritdoc />
    public bool Equals(FunctionSignatureNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return Name == other.Name
            && ReturnType == other.ReturnType
            && ArgumentNodes.Equals(other.ArgumentNodes);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is FunctionSignatureNode other && Equals(other));

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Name, (int)ReturnType, ArgumentNodes);

    public static bool operator ==(FunctionSignatureNode? left, FunctionSignatureNode? right)
        => Equals(left, right);

    public static bool operator !=(FunctionSignatureNode? left, FunctionSignatureNode? right)
        => !Equals(left, right);
}
