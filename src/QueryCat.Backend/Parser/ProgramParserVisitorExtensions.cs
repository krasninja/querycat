using Antlr4.Runtime.Tree;
using QueryCat.Backend.Logging;

namespace QueryCat.Backend.Parser;

/// <summary>
/// Extensions and helpers for <see cref="ProgramParserVisitor" />.
/// </summary>
internal static class ProgramParserVisitorExtensions
{
    #region Return single item

    public static TTarget Visit<TTarget>(
        this ProgramParserVisitor visitor,
        IParseTree tree)
    {
        var result = VisitMaybe<TTarget>(visitor, tree);
        if (result == null)
        {
            var invalidQueryText = tree.GetText();
            Logger.Instance.Fatal($"Invalid query: {invalidQueryText}");
            throw new InvalidOperationException("Parse tree value is not defined.");
        }
        return result;
    }

    public static TTarget Visit<TTarget>(
        this ProgramParserVisitor visitor,
        IParseTree? tree,
        TTarget @default)
        => visitor.VisitMaybe<TTarget>(tree) ?? @default;

    public static TTarget? VisitMaybe<TTarget>(
        this ProgramParserVisitor visitor,
        IParseTree? tree)
    {
        if (tree == null)
        {
            return default;
        }
        var result = visitor.Visit(tree);
        if (result == null)
        {
            return default;
        }
        if (result is TTarget target)
        {
            return target;
        }
        throw new InvalidOperationException(
            $"Cannot convert result of type {result.GetType().Name} to {typeof(TTarget).Name}.");
    }

    #endregion

    #region Return multiple items

    public static IEnumerable<TTarget> Visit<TTarget>(
        this ProgramParserVisitor visitor,
        IEnumerable<IParseTree> tree)
        => tree.Select(visitor.Visit<TTarget>);

    public static IEnumerable<TTarget> Visit<TTarget>(
        this ProgramParserVisitor visitor,
        IEnumerable<IParseTree> tree,
        TTarget @default)
        => tree.Select(s => visitor.VisitMaybe<TTarget>(s) ?? @default);

    public static IEnumerable<TTarget?> VisitMaybe<TTarget>(
        this ProgramParserVisitor visitor,
        IEnumerable<IParseTree> tree)
        => tree.Select(visitor.VisitMaybe<TTarget>);

    #endregion
}
