namespace QueryCat.Backend.Ast;

/// <summary>
/// Calls the callback on every node visit.
/// </summary>
public sealed class CallbackDelegateVisitor : DelegateVisitor
{
    /// <summary>
    /// The delegate to be called on every node visit.
    /// </summary>
    public Action<IAstNode, AstTraversal> Callback { get; set; } = (_, _) => { };

    /// <inheritdoc />
    public override void OnVisit(IAstNode node) => Callback.Invoke(node, AstTraversal);
}
