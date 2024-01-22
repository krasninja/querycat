using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Function is a subprogram with input arguments and optional output value.
/// </summary>
public interface IFunction
{
    /// <summary>
    /// Invocation delegate.
    /// </summary>
    FunctionDelegate Delegate { get; }

    /// <summary>
    /// Function name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Function description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Function return type.
    /// </summary>
    DataType ReturnType { get; }

    /// <summary>
    /// Optional type of object return type.
    /// </summary>
    public string ReturnObjectName { get; }

    /// <summary>
    /// Is aggregate function or standard function.
    /// </summary>
    bool IsAggregate { get; }

    /// <summary>
    /// Signature arguments.
    /// </summary>
    FunctionSignatureArgument[] Arguments { get; }
}
