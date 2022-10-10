namespace QueryCat.Backend.Ast.Nodes.Select;

public sealed class SelectColumnsSublistNameNode : SelectColumnsSublistNode
{
    public string SourceName { get; }

    public string ColumnName { get; }

    /// <inheritdoc />
    public override string Code => "column_name";

    /// <inheritdoc />
    public SelectColumnsSublistNameNode(string columnName, string sourceName)
    {
        ColumnName = columnName;
        SourceName = sourceName;
    }

    public SelectColumnsSublistNameNode(string columnName)
    {
        ColumnName = columnName;
        SourceName = string.Empty;
    }

    public SelectColumnsSublistNameNode(SelectColumnsSublistNameNode node) :
        this(node.SourceName, node.ColumnName)
    {
        node.CopyTo(this);
    }

    /// <inheritdoc />
    public override object Clone() => new SelectColumnsSublistNameNode(this);

    /// <inheritdoc />
    public override void Accept(AstVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override string ToString() => !string.IsNullOrEmpty(SourceName) ? $"{SourceName}.{ColumnName}" : ColumnName;
}
