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
        public override void Run(IAstNode node)
        {
        }

        /// <inheritdoc />
        public override void Visit(EmptyNode node)
        {
            Calls.Append(node.Value);
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
    public void PreOrder_Traverse_ShouldTraverseCorrectly()
    {
        // Act.
        _astTraversal.PreOrder(_root);

        // Assert.
        Assert.Equal("12453", _emptyNodesVisitor.Calls.ToString());
    }

    [Fact]
    public void PostOrder_Traverse_ShouldTraverseCorrectly()
    {
        // Act.
        _astTraversal.PostOrder(_root);

        // Assert.
        Assert.Equal("45231", _emptyNodesVisitor.Calls.ToString());
    }
}
