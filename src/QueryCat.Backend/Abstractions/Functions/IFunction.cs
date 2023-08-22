using QueryCat.Backend.Types;

namespace QueryCat.Backend.Abstractions.Functions;

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
    /// Is aggregate function or standard function.
    /// </summary>
    bool IsAggregate { get; }
}
