namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// The special attribute to specify aggregate functions.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AggregateFunctionSignatureAttribute : FunctionSignatureAttribute
{
    /// <inheritdoc />
    public AggregateFunctionSignatureAttribute(string signature) : base(signature)
    {
    }
}
