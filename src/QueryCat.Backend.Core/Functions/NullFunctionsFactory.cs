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
    public override IEnumerable<IFunction> CreateFromDelegate(Delegate functionDelegate) => [];

    /// <inheritdoc />
    public override IFunction CreateFromSignature(
        string signature,
        Delegate functionDelegate,
        FunctionMetadata? metadata = null) => NullFunction.Instance;

    /// <inheritdoc />
    public override IEnumerable<IFunction> CreateAggregateFromType<TAggregate>() => [];
}
