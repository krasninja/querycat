using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select;

internal sealed class SelectCommandHandler : CommandHandler
{
    public SelectCommandContext SelectCommandContext { get; }

    public SelectCommandHandler(SelectCommandContext selectCommandContext)
    {
        SelectCommandContext = selectCommandContext;
    }

    /// <inheritdoc />
    public override VariantValue Invoke()
    {
        return VariantValue.CreateFromObject(SelectCommandContext.CurrentIterator);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        SelectCommandContext.Dispose();
        base.Dispose(disposing);
    }
}
