using QueryCat.Backend.Ast;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Mcp.Utils;

internal static class QueryMissedConditionText
{
    public static string GetClientInstructionText(QueryMissedConditionException exception)
    {
        var sourceName = string.Empty;
        if (!string.IsNullOrEmpty(exception.InputSourceName))
        {
            if (exception.InputSourceType == QueryMissedConditionException.MissedConditionInputSourceType.Identifier)
            {
                sourceName = exception.InputSourceName;
            }
            else if (exception.InputSourceType == QueryMissedConditionException.MissedConditionInputSourceType.Function)
            {
                sourceName = $"{exception.InputSourceName}([arguments])";
            }
        }

        var whereText = $"{StringUtils.Quote(exception.ColumnName)} {QueryAstVisitor.GetOperationString(exception.Operations.First())} [value]";
        var sqlText = $"SELECT * FROM {sourceName} WHERE {whereText}";
        var requiredText =
            $"{exception.ColumnName} ({string.Join(' ', exception.Operations.Select(QueryAstVisitor.GetOperationString))})";

        return string.Format(Resources.Errors.McpMissedRequiredOperation, sqlText, requiredText);
    }
}
