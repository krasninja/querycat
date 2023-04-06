using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast.Nodes;
using QueryCat.Backend.Ast.Nodes.Insert;
using QueryCat.Backend.Ast.Nodes.Select;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Commands.Insert;

internal class InsertCommand : ICommand
{
    /// <inheritdoc />
    public CommandHandler CreateHandler(ExecutionThread executionThread, StatementNode node)
    {
        var insertNode = (InsertNode)node.RootNode;

        // Evaluate iterator for FROM block and get input source.
        var iterator =new SelectPlanner(executionThread).CreateIterator(insertNode.QueryNode);
        var targetColumns = GetTargetColumns(iterator, insertNode);

        // Get output source.
        var rowsOutput = new CreateDelegateVisitor(executionThread)
            .RunAndReturn(insertNode.InsertTargetNode)
            .Invoke()
            .GetAsObject<IRowsOutput>();

        // Find correspond from-to columns and create mapping.
        var mapIterator = CreateFromToColumnsMapping(iterator, targetColumns);

        // Handler.
        return new InsertCommandHandler(executionThread, mapIterator, rowsOutput);
    }

    private static MapRowsIterator CreateFromToColumnsMapping(IRowsIterator inputIterator, List<string> targetColumns)
    {
        var mapIterator = new MapRowsIterator(inputIterator);
        foreach (var columnName in targetColumns)
        {
            var index = inputIterator.GetColumnIndexByName(columnName);
            if (index < 0)
            {
                throw new QueryCatException($"Cannot find column '{columnName}'.");
            }
            mapIterator.Add(index);
        }
        return mapIterator;
    }

    private static List<string> GetTargetColumns(
        IRowsIterator inputIterator,
        InsertNode insertNode)
    {
        List<string> targetColumns;
        if (insertNode.ColumnsNode != null)
        {
            targetColumns = insertNode.ColumnsNode.Columns;
        }
        else
        {
            return inputIterator.Columns.Select(c => c.Name).ToList();
        }

        // Check two patterns:
        // - INSERT INTO x() (a, b) SELECT 1, 2
        // - INSERT INTO x() (a, b) SELECT * FROM VALUES (1, 2), (3, 4), ...
        var getColumnsByIndex =
            insertNode.QueryNode is SelectQuerySpecificationNode querySpecificationNode
            && (querySpecificationNode.TableExpressionNode == null
                || querySpecificationNode.TableExpressionNode?.TablesNode.TableFunctionsNodes.First() is SelectTableNode);
        if (getColumnsByIndex)
        {
            for (var i = 0; i < targetColumns.Count && i < inputIterator.Columns.Length; i++)
            {
                inputIterator.Columns[i].Name = targetColumns[i];
            }
        }

        return targetColumns;
    }
}
