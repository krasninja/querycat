namespace QueryCat.Backend.Functions;

/// <summary>
/// Attribute to specify QueryCat function.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class FunctionSignatureAttribute : Attribute
{
    public string Signature { get; }

    public FunctionSignatureAttribute(string signature)
    {
        Signature = signature;
    }

    /// <inheritdoc />
    public override string ToString() => Signature;
}
