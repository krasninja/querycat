namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Attribute to specify QueryCat function.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class FunctionSignatureAttribute : Attribute
{
    public string Signature { get; } = string.Empty;

    /// <summary>
    /// Constructor. The signature will be determined from type.
    /// </summary>
    public FunctionSignatureAttribute()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="signature">Function signature.</param>
    public FunctionSignatureAttribute(string signature)
    {
        Signature = signature;
    }

    /// <inheritdoc />
    public override string ToString() => Signature;
}
