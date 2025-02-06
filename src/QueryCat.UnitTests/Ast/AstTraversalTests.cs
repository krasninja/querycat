using System.Text;
using Xunit;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes;

namespace QueryCat.UnitTests.Ast;

/// <summary>
/// Tests for <see cref="AstTraversal" />.
/// </summary>
public class AstTraversalTests
{
    /*
     *       1
     *    2     3
     *  4  5
     */

    private readonly EmptyNode _root;
    private readonly EmptyNodesVisitor _emptyNodesVisitor = new();
    private readonly AstTraversal _astTraversal;

    private class EmptyNodesVisitor : AstVisitor
    {
        public StringBuilder Calls { get; } = new();

        /// <inheritdoc />
        public override ValueTask RunAsync(IAstNode node, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public override ValueTask VisitAsync(EmptyNode node, CancellationToken cancellationToken)
        {
            Calls.Append(node.Value);
            return ValueTask.CompletedTask;
        }
    }

    public AstTraversalTests()
    {
        _root = new EmptyNode("1",
            new EmptyNode("2",
                new EmptyNode("4"), new EmptyNode("5")),
            new EmptyNode("3"));
        _astTraversal = new AstTraversal(_emptyNodesVisitor);
    }

    [Fact]
    public async Task PreOrder_Traverse_ShouldTraverseCorrectly()
    {
        // Act.
        await _astTraversal.PreOrderAsync(_root, CancellationToken.None);

        // Assert.
        Assert.Equal("12453", _emptyNodesVisitor.Calls.ToString());
    }

    [Fact]
    public async Task PostOrder_Traverse_ShouldTraverseCorrectly()
    {
        // Act.
        await _astTraversal.PostOrderAsync(_root, CancellationToken.None);

        // Assert.
        Assert.Equal("45231", _emptyNodesVisitor.Calls.ToString());
    }
}
