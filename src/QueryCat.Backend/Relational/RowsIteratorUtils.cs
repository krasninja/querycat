using System.Text;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Utils for <see cref="IRowsIterator" />.
/// </summary>
public static class RowsIteratorUtils
{
    /// <summary>
    /// Read the rows from iterator and try to determine better type for string columns.
    /// For example, if column contains only numbers the rows set will set "integer" data type for it.
    /// </summary>
    /// <param name="rowsIterator">Rows iterator to read.</param>
    /// <param name="numberOfRowsToAnalyze">Number of rows to analyze, default is 10.</param>
    /// <param name="skipColumnsIndexes">Skip certain columns by indexes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task ResolveColumnsTypesAsync(
        IRowsIterator rowsIterator,
        int numberOfRowsToAnalyze = 10,
        int[]? skipColumnsIndexes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfRowsToAnalyze);
        skipColumnsIndexes ??= [];

        // Read.
        var rowsFrame = await ReadRowsFrameAsync(rowsIterator, numberOfRowsToAnalyze, cancellationToken);
        if (rowsFrame == null)
        {
            return;
        }

        // Analyze and create new columns.
        for (var i = 0; i < rowsFrame.Columns.Length; i++)
        {
            var column = rowsFrame.Columns[i];
            if (column.DataType != DataType.String || skipColumnsIndexes.Contains(i))
            {
                continue;
            }

            var values = rowsFrame.GetColumnValues(i);
            var newType = DataTypeUtils.DetermineTypeByValues(values);
            column.DataType = newType;
            rowsIterator.Columns[i].DataType = newType;
        }
    }

    /// <summary>
    /// Contains simple analyze logic to determine if there is a header row
    /// in rows set. It cannot be 100% true.
    /// </summary>
    /// <param name="rowsIterator">Rows iterator.</param>
    /// <param name="numberOfRowsToAnalyze">First number of rows to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>True</c> if it possibly contains a header, <c>false</c> otherwise.</returns>
    public static async Task<bool> DetermineIfHasHeaderAsync(IRowsIterator rowsIterator, int numberOfRowsToAnalyze = 10,
        CancellationToken cancellationToken = default)
    {
        if (rowsIterator.Columns.Length < 1)
        {
            return false;
        }

        // Probably it should be something like this:
        // https://github.com/python/cpython/blob/main/Lib/csv.py#L390.

        var rowsFrame = new RowsFrame(rowsIterator.Columns);
        var countDown = numberOfRowsToAnalyze;
        while (countDown > 0 && await rowsIterator.MoveNextAsync(cancellationToken))
        {
            rowsFrame.AddRow(rowsIterator.Current);
            countDown--;
        }

        if (rowsFrame.TotalRows < 2)
        {
            return false;
        }

        var hasHeader = 0;

        for (var columnIndex = 0; columnIndex < rowsFrame.Columns.Length; columnIndex++)
        {
            var values = rowsFrame.GetColumnValues(columnIndex);
            var firstValue = values.First();
            var headerType = DataTypeUtils.DetermineTypeByValues([firstValue]);
            var rowsType = DataTypeUtils.DetermineTypeByValues(values.Skip(1));

            // If there are more than 70% empty values - probably it is a bad header column. Skip if first value
            // is not empty.
            if (string.IsNullOrWhiteSpace(firstValue))
            {
                var emptyValuesPercent = (float)values.Count(v => IsEmpty(v)) / values.Count;
                if (emptyValuesPercent > 0.7f)
                {
                    hasHeader--;
                    continue;
                }
            }

            // If the first row is string, but others are not - probably this is a header.
            if (headerType == DataType.String && rowsType != DataType.String)
            {
                hasHeader++;
            }
            else if (headerType == DataType.Boolean || headerType == DataType.Timestamp)
            {
                hasHeader--;
            }
            else if (rowsFrame.Columns.Length < 3 && headerType == rowsType
                     && (headerType == DataType.Float || headerType == DataType.Integer || headerType == DataType.Numeric))
            {
                hasHeader--;
            }
        }

        // By default, consider this as header.
        return hasHeader >= 0;
    }

    private static bool IsEmpty(in VariantValue value)
        => value.IsNull || (value.Type == DataType.String && string.IsNullOrEmpty(value.AsStringUnsafe));

    // Column names below are in normalized form (see NormalizeColumnName).

    private static readonly HashSet<string> _idColumnNames =
    [
        "id", "identifier", "uuid", "guid", "pk",
        "eventid", "logid", "recordid", "rowid",
    ];

    private static readonly HashSet<string> _traceColumnNames =
    [
        "traceid", "spanid", "parentspanid", "requestid", "correlationid", "sessionid",
        "operationid", "activityid", "transactionid",
    ];

    // Ordered by priority: the first matching column gets the attribute.
    private static readonly string[] _timestampColumnNames =
    [
        "@timestamp", "timestamp", "@ts", "ts",
        "eventtime", "occurredat", "loggedat", "logtime",
        "time", "datetime", "date",
        "createdat", "created", "createdtime", "eventdate",
        "receivedat", "recordedat",
    ];

    /// <summary>
    /// Analyze columns names and values and assume what attributes to add. Attributes
    /// already set by the input are preserved.
    /// </summary>
    /// <param name="rowsIterator">Rows iterator to read.</param>
    /// <param name="numberOfRowsToAnalyze">Number of rows to analyze, default is 10.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task ResolveColumnsAttributesAsync(
        IRowsIterator rowsIterator,
        int numberOfRowsToAnalyze = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(numberOfRowsToAnalyze);

        var rowsFrame = await ReadRowsFrameAsync(rowsIterator, numberOfRowsToAnalyze, cancellationToken);
        var columns = rowsIterator.Columns;

        var hasId = columns.Any(c => c.HasAttributes && c.Attributes.ContainsKey(Column.AttributeId));
        var hasTimestamp = columns.Any(c => c.HasAttributes && c.Attributes.ContainsKey(Column.AttributeTimestamp));
        var timestampColumnIndex = -1;
        var timestampPriority = int.MaxValue;

        for (var i = 0; i < columns.Length; i++)
        {
            var column = columns[i];
            if (column.HasAttributes
                && (column.Attributes.ContainsKey(Column.AttributeId)
                    || column.Attributes.ContainsKey(Column.AttributeTrace)
                    || column.Attributes.ContainsKey(Column.AttributeTimestamp)))
            {
                continue;
            }
            var name = NormalizeColumnName(column.Name);

            // id.
            if (!hasId && _idColumnNames.Contains(name) && HasUniqueValues(rowsFrame, i))
            {
                column.SetAttribute(Column.ColumnAttributeId);
                hasId = true;
                continue;
            }

            // trace.
            if (_traceColumnNames.Contains(name))
            {
                column.SetAttribute(Column.ColumnAttributeTrace);
                continue;
            }

            // timestamp.
            if (!hasTimestamp && column.DataType == DataType.Timestamp)
            {
                var priority = Array.IndexOf(_timestampColumnNames, name);
                if (priority > -1 && priority < timestampPriority)
                {
                    timestampPriority = priority;
                    timestampColumnIndex = i;
                }
            }
        }
        if (timestampColumnIndex > -1)
        {
            columns[timestampColumnIndex].SetAttribute(Column.ColumnAttributeTimestamp);
        }
    }

    private static async Task<RowsFrame?> ReadRowsFrameAsync(
        IRowsIterator rowsIterator,
        int numberOfRows,
        CancellationToken cancellationToken)
    {
        RowsFrame? rowsFrame = null;
        for (var rowIndex = 0; rowIndex < numberOfRows && await rowsIterator.MoveNextAsync(cancellationToken); rowIndex++)
        {
            // In some rows iterators the Columns property is initialized during first MoveNext() call,
            // so we postpone rows frame initialization.
            rowsFrame ??= new RowsFrame(rowsIterator.Columns);
            rowsFrame.AddRow(rowsIterator.Current);
        }
        return rowsFrame;
    }

    /// <summary>
    /// Returns <c>true</c> if sample values of the column are non-empty and distinct.
    /// If there is no data to analyze, it returns <c>true</c>.
    /// </summary>
    private static bool HasUniqueValues(RowsFrame? rowsFrame, int columnIndex)
    {
        if (rowsFrame == null)
        {
            return true;
        }

        var values = rowsFrame.GetColumnValues(columnIndex);
        var set = new HashSet<VariantValue>(values.Count);
        foreach (var value in values)
        {
            if (IsEmpty(value) || !set.Add(value))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Lower-case the column name and remove separators, so "createdAt", "Created At",
    /// "created-at" and "created_at" become "createdat".
    /// </summary>
    private static string NormalizeColumnName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (ch is '_' or '-' or ' ' or '.')
            {
                continue;
            }
            sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }
}
