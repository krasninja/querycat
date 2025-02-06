namespace QueryCat.Backend.Ast;

/// <summary>
/// Calls the callback on every node visit.
/// </summary>
internal sealed class CallbackDelegateVisitor : DelegateVisitor
{
    /// <summary>
    /// The delegate to be called on every node visit.
    /// </summary>
    public Func<IAstNode, AstTraversal, CancellationToken, ValueTask> Callback { get; set; }
        = (node, astTraversal, ct) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public override ValueTask OnVisitAsync(IAstNode node, CancellationToken cancellationToken)
        => Callback.Invoke(node, AstTraversal, cancellationToken);
}
