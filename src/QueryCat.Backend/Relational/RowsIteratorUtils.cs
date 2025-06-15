using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Utils for <see cref="IRowsIterator" />.
/// </summary>
public static class RowsIteratorUtils
{
    private sealed class EmptyRowsIterator : IRowsIterator
    {
        /// <inheritdoc />
        public Column[] Columns => [];

        /// <inheritdoc />
        public Row Current { get; }

        public EmptyRowsIterator()
        {
            Current = new Row(this);
        }

        /// <inheritdoc />
        public ValueTask<bool> MoveNextAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(false);

        /// <inheritdoc />
        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Explain(IndentedStringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Empty");
        }
    }

    /// <summary>
    /// Empty rows iterator.
    /// </summary>
    public static IRowsIterator Empty => new EmptyRowsIterator();

    /// <summary>
    /// Read the rows from iterator and try to determine better type for string columns.
    /// For example, if column contains only numbers the new rows set will set "integer" data type for it.
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
        skipColumnsIndexes ??= [];

        // Read.
        RowsFrame? rowsFrame = null;
        for (var rowIndex = 0; rowIndex < numberOfRowsToAnalyze && await rowsIterator.MoveNextAsync(cancellationToken); rowIndex++)
        {
            // In some rows iterators the Columns property is initialized during first MoveNext() call,
            // so we postpone rows frame initialization.
            rowsFrame ??= new RowsFrame(rowsIterator.Columns);
            rowsFrame.AddRow(rowsIterator.Current);
        }
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
        // Probably it should be something like this:
        // https://github.com/python/cpython/blob/main/Lib/csv.py#L390.

        var rowsFrame = new RowsFrame(rowsIterator.Columns);
        var countDown = numberOfRowsToAnalyze;
        while (await rowsIterator.MoveNextAsync(cancellationToken) && countDown > 0)
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
            var headerType = DataTypeUtils.DetermineTypeByValues([values.First()]);
            var rowsType = DataTypeUtils.DetermineTypeByValues(values.Skip(1));

            // If there are more than 70% empty values - probably it is a bad header column. Skip if first value
            // is not empty.
            if (string.IsNullOrWhiteSpace(values.First()))
            {
                var emptyValuesPercent = (float)values.Count(v => string.IsNullOrWhiteSpace(v)) / values.Count;
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
}
