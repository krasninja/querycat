using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Empty function.
/// </summary>
public sealed class NullFunction : IFunction
{
    /// <summary>
    /// Instance of <see cref="NullFunction" />.
    /// </summary>
    public static IFunction Instance { get; } = new NullFunction();

    /// <inheritdoc />
    public Delegate Delegate => (IExecutionThread thread) => VariantValue.Null;

    /// <inheritdoc />
    public string Name => "NULL";

    /// <inheritdoc />
    public string Description => "Empty function.";

    /// <inheritdoc />
    public DataType ReturnType => DataType.Void;

    /// <inheritdoc />
    public string ReturnObjectName => string.Empty;

    /// <inheritdoc />
    public bool IsAggregate => false;

    /// <inheritdoc />
    public FunctionSignatureArgument[] Arguments => [];

    /// <inheritdoc />
    public bool IsSafe => true;

    /// <inheritdoc />
    public string[] Formatters => [];
}
