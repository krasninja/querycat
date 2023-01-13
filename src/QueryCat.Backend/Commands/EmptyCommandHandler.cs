using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Empty implementation for <see cref="CommandHandler" />.
/// </summary>
public class EmptyCommandHandler : CommandHandler
{
    public static EmptyCommandHandler Empty { get; } = new();

    /// <inheritdoc />
    public override VariantValue Invoke()
    {
        return VariantValue.Null;
    }
}
