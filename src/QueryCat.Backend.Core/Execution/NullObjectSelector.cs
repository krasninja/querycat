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
    public ValueTask<ObjectSelectorContext.Token?> SelectByPropertyAsync(
        ObjectSelectorContext context,
        string propertyName,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult((ObjectSelectorContext.Token?)null);

    /// <inheritdoc />
    public ValueTask<ObjectSelectorContext.Token?> SelectByIndexAsync(
        ObjectSelectorContext context,
        object?[] indexes,
        CancellationToken cancellationToken = default) => ValueTask.FromResult((ObjectSelectorContext.Token?)null);

    /// <inheritdoc />
    public ValueTask<bool> SetValueAsync(
        ObjectSelectorContext context,
        object? newValue,
        CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
}
