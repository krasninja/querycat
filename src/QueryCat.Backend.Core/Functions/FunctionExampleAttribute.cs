namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// The attribute shows the example function call or using it as part of SQL query.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class FunctionExampleAttribute : Attribute
{
    /// <summary>
    /// Example usage.
    /// </summary>
    public string Example { get; }

    /// <inheritdoc />
    public FunctionExampleAttribute(string example)
    {
        Example = example;
    }
}
