namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Functions factory without any behavior.
/// </summary>
public class NullFunctionsFactory : FunctionsFactory
{
    /// <summary>
    /// Empty instance.
    /// </summary>
    public static FunctionsFactory Instance { get; } = new NullFunctionsFactory();

    /// <inheritdoc />
    public override IFunction[] CreateFromDelegate(Delegate functionDelegate) => [];

    /// <inheritdoc />
    public override IFunction CreateFromSignature(
        string signature,
        Delegate functionDelegate,
        string? description = null,
        bool isSafe = false,
        string[]? formatters = null) => NullFunction.Instance;

    /// <inheritdoc />
    public override IFunction[] CreateAggregateFromType(Type aggregateType) => [];
}
