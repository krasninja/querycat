using QueryCat.Backend.Core.Types;

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
    public void PushObjectByProperty(ObjectSelectorContext context, string propertyName)
    {
    }

    /// <inheritdoc />
    public void PushObjectByIndex(ObjectSelectorContext context, VariantValue[] indexes)
    {
    }
}
