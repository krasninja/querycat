using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Functions;

/// <summary>
/// The special attribute to specify aggregate functions.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AggregateFunctionSignatureAttribute : FunctionSignatureAttribute
{
    /// <inheritdoc />
    public AggregateFunctionSignatureAttribute(string signature) : base(signature)
    {
    }
}
