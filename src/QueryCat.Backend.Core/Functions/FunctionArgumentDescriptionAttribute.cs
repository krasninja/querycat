namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// The attribute adds description to function argument.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class FunctionArgumentDescriptionAttribute : Attribute
{
    /// <summary>
    /// Argument name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Argument description.
    /// </summary>
    public string Description { get; }

    /// <inheritdoc />
    public FunctionArgumentDescriptionAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
