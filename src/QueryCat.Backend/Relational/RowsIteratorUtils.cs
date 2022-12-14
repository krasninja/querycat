using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Utils for <see cref="IRowsIterator" />.
/// </summary>
public static class RowsIteratorUtils
{
    private sealed class EmptyRowsIterator : IRowsIterator
    {
        /// <inheritdoc />
        public Column[] Columns => Array.Empty<Column>();

        /// <inheritdoc />
        public Row Current { get; }

        public EmptyRowsIterator()
        {
            Current = new Row(this);
        }

        /// <inheritdoc />
        public bool MoveNext() => false;

        /// <inheritdoc />
        public void Reset()
        {
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
    public static void ResolveColumnsTypes(IRowsIterator rowsIterator, int numberOfRowsToAnalyze = 10)
    {
        // Read.
        RowsFrame? rowsFrame = null;
        for (var rowIndex = 0; rowIndex < numberOfRowsToAnalyze && rowsIterator.MoveNext(); rowIndex++)
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
            var values = rowsFrame.GetColumnValues(i);
            var newType = DetermineTypeByValues(values);
            column.DataType = newType;
        }
    }

    internal static DataType DetermineTypeByValues(IEnumerable<string> values)
        => DetermineTypeByValues(values.Select(v => new VariantValue(v)));

    /// <summary>
    /// Try to guess optimal type by list of values. Null values are skipped.
    /// The default type is string.
    /// </summary>
    /// <param name="values">List of values.</param>
    /// <returns>Optimal type.</returns>
    internal static DataType DetermineTypeByValues(IEnumerable<VariantValue> values)
    {
        var variantValues = values
            .Where(v => !string.IsNullOrEmpty(v.AsString))
            .ToArray();

        bool TestType(DataType dataType)
        {
            var wasTested = false;
            foreach (var variantValue in variantValues)
            {
                if (string.IsNullOrEmpty(variantValue.AsString))
                {
                    continue;
                }
                wasTested = true;
                if (!variantValue.TryCast(dataType, out _))
                {
                    return false;
                }
            }
            return wasTested;
        }

        if (TestType(DataType.Integer))
        {
            return DataType.Integer;
        }
        if (TestType(DataType.Float))
        {
            return DataType.Float;
        }
        if (TestType(DataType.Boolean))
        {
            return DataType.Boolean;
        }
        if (TestType(DataType.Timestamp))
        {
            return DataType.Timestamp;
        }

        return DataType.String;
    }

    /// <summary>
    /// Contains simple analyze logic to determine if there is a header row
    /// in rows set. It cannot be 100% true.
    /// </summary>
    /// <param name="rowsIterator">Rows iterator.</param>
    /// <param name="numberOfRowsToAnalyze">First number of rows to analyze.</param>
    /// <returns><c>True</c> if it possibly contains a header, <c>false</c> otherwise.</returns>
    public static bool DetermineIfHasHeader(IRowsIterator rowsIterator, int numberOfRowsToAnalyze = 10)
    {
        // Probably it should be something like this:
        // https://github.com/python/cpython/blob/main/Lib/csv.py#L392.

        var rowsFrame = new RowsFrame(rowsIterator.Columns);
        var countDown = numberOfRowsToAnalyze;
        while (rowsIterator.MoveNext() && countDown > 0)
        {
            rowsFrame.AddRow(rowsIterator.Current);
            countDown--;
        }

        if (rowsFrame.TotalRows < 2)
        {
            return false;
        }

        int hasHeader = 0;

        for (var columnIndex = 0; columnIndex < rowsFrame.Columns.Length; columnIndex++)
        {
            var values = rowsFrame.GetColumnValues(columnIndex);
            var headerType = DetermineTypeByValues(new[] { values.First() });
            var rowsType = DetermineTypeByValues(values.Skip(1));

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

        // By default consider this as header.
        return hasHeader >= 0;
    }
}
