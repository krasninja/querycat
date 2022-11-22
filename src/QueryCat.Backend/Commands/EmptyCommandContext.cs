using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Empty implementation for <see cref="CommandContext" />.
/// </summary>
public class EmptyCommandContext : CommandContext
{
    public static EmptyCommandContext Empty { get; } = new();

    /// <inheritdoc />
    public override VariantValue Invoke()
    {
        return VariantValue.Null;
    }
}
