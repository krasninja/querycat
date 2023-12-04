using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Empty implementation for <see cref="CommandHandler" />.
/// </summary>
internal sealed class EmptyCommandHandler : CommandHandler
{
    public static EmptyCommandHandler Empty { get; } = new();

    /// <inheritdoc />
    public override VariantValue Invoke()
    {
        return VariantValue.Null;
    }
}
