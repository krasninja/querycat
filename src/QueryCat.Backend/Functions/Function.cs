using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Custom .NET function that can be invoked from query.
/// </summary>
public class Function
{
    /// <summary>
    /// Invocation delegate.
    /// </summary>
    public FunctionDelegate Delegate { get; }

    private readonly FunctionSignatureNode _signatureNode;

    /// <summary>
    /// Function name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Function description.
    /// </summary>
    public string Description { get; internal set; } = string.Empty;

    /// <summary>
    /// Function return type.
    /// </summary>
    public DataType ReturnType => _signatureNode.ReturnType;

    /// <summary>
    /// Optional type of object return type.
    /// </summary>
    public string ReturnObjectName => _signatureNode.ReturnTypeNode.TypeName;

    /// <summary>
    /// Can the function be used for aggregate queries. Aggregate queries requires state and initial value.
    /// </summary>
    public bool IsAggregate { get; }

    /// <summary>
    /// Arguments.
    /// </summary>
    public FunctionSignatureArgumentNode[] Arguments => _signatureNode.ArgumentNodes;

    public static Function Empty => new(
        _ => VariantValue.Null, new FunctionSignatureNode("Empty", FunctionTypeNode.NullTypeInstance));

    internal Function(FunctionDelegate @delegate, FunctionSignatureNode signatureNode,
        bool aggregate = false)
    {
        Delegate = @delegate;
        _signatureNode = signatureNode;
        Name = _signatureNode.Name;
        IsAggregate = aggregate;
    }

    internal bool MatchesToArguments(FunctionArgumentsTypes functionArgumentsTypes)
    {
        var argumentsNodes = Arguments;
        var variadicArgument = argumentsNodes.Length > 0 && argumentsNodes[^1].IsVariadic
            ? argumentsNodes[^1] : null;

        var typesArr = new DataType[argumentsNodes.Length];
        Array.Fill(typesArr, DataType.Void);

        // Fill function arguments types.
        for (int i = 0; i < argumentsNodes.Length; i++)
        {
            if (argumentsNodes[i].HasDefaultValue || argumentsNodes[i].IsOptional)
            {
                typesArr[i] = argumentsNodes[i].TypeNode.Type;
            }
        }
        if (functionArgumentsTypes.Positional.Length > typesArr.Length && variadicArgument == null)
        {
            return false;
        }

        // Place positional arguments.
        for (int i = 0; i < functionArgumentsTypes.Positional.Length; i++)
        {
            var arg = functionArgumentsTypes.Positional[i];
            if (arg.Key > typesArr.Length - 1)
            {
                if (variadicArgument != null)
                {
                    Array.Resize(ref typesArr, arg.Key + 1);
                    Array.Resize(ref argumentsNodes, arg.Key + 1);
                    argumentsNodes[^1] = (FunctionSignatureArgumentNode)variadicArgument.Clone();
                }
                else
                {
                    return false;
                }
            }
            typesArr[arg.Key] = arg.Value;
        }

        // Place named arguments.
        for (int i = 0; i < functionArgumentsTypes.Named.Length; i++)
        {
            var argumentPosition = GetArgumentPosition(functionArgumentsTypes.Named[i].Key);
            typesArr[argumentPosition] = functionArgumentsTypes.Named[i].Value;
        }

        // Make sure we resolved all arguments nodes.
        for (int i = 0; i < typesArr.Length; i++)
        {
            // For variadic we can place any number of arguments.
            if (argumentsNodes[i] == variadicArgument)
            {
                continue;
            }

            // The VOID data type is used with ANY argument type.
            if (argumentsNodes[i].TypeNode.Type != DataType.Void && typesArr[i] != argumentsNodes[i].TypeNode.Type)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get argument position.
    /// </summary>
    /// <param name="name">Argument name.</param>
    /// <returns>Position.</returns>
    public int GetArgumentPosition(string name)
    {
        name = name.ToUpper();
        var argumentPosition = Array.FindIndex(Arguments, node => node.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (argumentPosition < 0)
        {
            throw new CannotFindArgumentException(Name, name);
        }
        return argumentPosition;
    }

    public bool IsSignatureEquals(Function function)
        => _signatureNode.Equals(function._signatureNode);

    /// <inheritdoc />
    public override string ToString() => _signatureNode.ToString();
}
