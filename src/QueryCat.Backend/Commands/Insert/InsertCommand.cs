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

        // Get output source.
        var rowsOutput = new CreateDelegateVisitor(executionThread)
            .RunAndReturn(insertNode.InsertTargetNode)
            .Invoke()
            .As<IRowsOutput>();

        // Evaluate iterator for FROM block and get input source.
        var outputDefinedColumns = insertNode.HasDefinedColumns();
        var inputDefinedColumns = insertNode.QueryNode.ColumnsListNode.HasDefinedColumns();
        if (!outputDefinedColumns && inputDefinedColumns)
        {
            CopyInputColumnsToOutput(insertNode.QueryNode.ColumnsListNode, insertNode);
        }
        var inputIterator = new SelectPlanner(executionThread).CreateIterator(insertNode.QueryNode);

        // If input list is not defined but for output we have columns - use them as target.
        if (rowsOutput is IRowsSchema rowsOutputSchema && !inputDefinedColumns)
        {
            for (var i = 0; i < rowsOutputSchema.Columns.Length && i < inputIterator.Columns.Length; i++)
            {
                inputIterator.Columns[i].Name = rowsOutputSchema.Columns[i].Name;
                inputIterator.Columns[i].SourceName = rowsOutputSchema.Columns[i].SourceName;
            }
        }

        // Find correspond from-to columns and create mapping.
        var targetColumns = GetTargetColumns(inputIterator, insertNode);
        var mapIterator = CreateFromToColumnsMappingByName(inputIterator, rowsOutput, targetColumns);

        // Handler.
        return new InsertCommandHandler(executionThread, mapIterator, rowsOutput);
    }

    private static void CopyInputColumnsToOutput(SelectColumnsListNode inputColumnsListNode, InsertNode insertNode)
    {
        var targetColumns = new InsertColumnsListNode();
        foreach (var columnNode in inputColumnsListNode.ColumnsNodes)
        {
            if (columnNode is SelectColumnsSublistExpressionNode columnsSublistNode
                && columnsSublistNode.ExpressionNode is IdentifierExpressionNode identifierExpressionNode)
            {
                targetColumns.Columns.Add(identifierExpressionNode.FullName);
            }
        }
        insertNode.ColumnsNode = targetColumns;
    }

    private static MapRowsIterator CreateFromToColumnsMappingByName(
        IRowsIterator inputIterator,
        IRowsOutput rowsOutput,
        List<string> targetColumns)
    {
        var outputColumns = rowsOutput as IRowsSchema;
        var mapIterator = new MapRowsIterator(inputIterator);
        foreach (var columnName in targetColumns)
        {
            var index = inputIterator.GetColumnIndexByName(columnName);
            if (index < 0)
            {
                throw new QueryCatException($"Cannot find column '{columnName}'.");
            }
            if (outputColumns != null)
            {
                var outputColumn = outputColumns.GetColumnByName(columnName);
                if (outputColumn != null)
                {
                    mapIterator.Add(index, outputColumn.DataType);
                    continue;
                }
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
