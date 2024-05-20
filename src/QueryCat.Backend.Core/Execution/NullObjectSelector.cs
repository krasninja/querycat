namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Object selection without any functionality.
/// </summary>
public sealed class NullObjectSelector : IObjectSelector
{
    /// <summary>
    /// Instance of <see cref="NullObjectSelector" />.
    /// </summary>
    public static NullObjectSelector Instance { get; } = new();

    /// <inheritdoc />
    public ObjectSelectorContext.Token? SelectByProperty(ObjectSelectorContext context, string propertyName) => null;

    /// <inheritdoc />
    public ObjectSelectorContext.Token? SelectByIndex(ObjectSelectorContext context, object?[] indexes) => null;

    /// <inheritdoc />
    public bool SetValue(ObjectSelectorContext context, object? newValue) => false;
}
