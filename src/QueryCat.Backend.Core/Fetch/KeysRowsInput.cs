using System.Diagnostics;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Rows input with keys columns support. It has the separate list of columns
/// that contains key values. These values (with correspond conditions) can be used to optimize read operation.
/// </summary>
public abstract class KeysRowsInput : RowsInput, IDisposable
{
    [DebuggerDisplay("{KeyColumnIndex} => {Operation} {Value}")]
    private sealed class KeyColumnValue(int keyColumnIndex)
    {
        public int KeyColumnIndex { get; } = keyColumnIndex;

        public VariantValue Value { get; private set; }

        public bool IsSet { get; private set; }

        public VariantValue.Operation Operation { get; private set; } = VariantValue.Operation.Equals;

        public void Set(VariantValue value, VariantValue.Operation operation)
        {
            IsSet = true;
            Value = value;
            Operation = operation;
        }

        public void Unset()
        {
            IsSet = false;
            Value = VariantValue.Null;
        }
    }

    private sealed class KeyColumnColumnIndexComparer : IComparer<KeyColumn>
    {
        public static KeyColumnColumnIndexComparer Instance { get; } = new();

        public int Compare(KeyColumn? x, KeyColumn? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }
            if (y is null)
            {
                return 1;
            }
            if (x is null)
            {
                return -1;
            }

            return x.ColumnIndex.CompareTo(y.ColumnIndex);
        }
    }

    private KeyColumn[] _keyColumns = [];
    private readonly IDictionary<int, KeyColumnValue[]> _setKeyColumns = new SortedDictionary<int, KeyColumnValue[]>();

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = [];

    protected Fetcher<TClass> CreateFetcher<TClass>() where TClass : class
    {
        var fetcher = new Fetcher<TClass>();
        var queryLimit = QueryContext.QueryInfo.Limit + QueryContext.QueryInfo.Offset;
        if (queryLimit.HasValue)
        {
            fetcher.Limit = Math.Min((int)queryLimit.Value, fetcher.Limit);
        }
        return fetcher;
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _setKeyColumns.Clear();
        await CloseAsync(cancellationToken);
        await base.ResetAsync(cancellationToken);
    }

    #region IRowsInputKeys

    /// <inheritdoc />
    public override IReadOnlyList<KeyColumn> GetKeyColumns() => _keyColumns;

    /// <inheritdoc />
    public override void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        var kcv = GetOrCreateKeyColumnValue(columnIndex, operation);
        kcv.Set(value, operation);
    }

    /// <inheritdoc />
    public override void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        var kcv = GetOrCreateKeyColumnValue(columnIndex, operation);
        kcv.Unset();
    }

    private KeyColumn GetKeyColumn(int columnIndex, VariantValue.Operation? operation)
    {
        foreach (var keyColumn in _keyColumns)
        {
            if (keyColumn.ColumnIndex == columnIndex && (!operation.HasValue || keyColumn.ContainsOperation(operation.Value)))
            {
                return keyColumn;
            }
            if (keyColumn.ColumnIndex > columnIndex)
            {
                break;
            }
        }
        throw new InvalidOperationException($"Cannot find column with index '{columnIndex}' and operation '{operation}' column.");
    }

    private KeyColumnValue GetOrCreateKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        KeyColumnValue? kcv;
        KeyColumn? keyColumn;
        if (!_setKeyColumns.TryGetValue(columnIndex, out var keyColumnValues))
        {
            keyColumn = GetKeyColumn(columnIndex, operation);
            kcv = new KeyColumnValue(keyColumn.ColumnIndex);
            _setKeyColumns[columnIndex] = [kcv];
            return kcv;
        }

        foreach (var keyColumnValue in keyColumnValues)
        {
            if (keyColumnValue.Operation == operation)
            {
                return keyColumnValue;
            }
        }

        keyColumn = GetKeyColumn(columnIndex, operation);
        kcv = new KeyColumnValue(keyColumn.ColumnIndex);
        Array.Resize(ref keyColumnValues, keyColumnValues.Length + 1);
        _setKeyColumns[columnIndex] = keyColumnValues;
        keyColumnValues[^1] = kcv;
        return kcv;
    }

    #endregion

    #region Key columns

    /// <summary>
    /// Get key column value by column name.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Operation.</param>
    /// <returns>Value or null.</returns>
    public VariantValue GetKeyColumnValue(string columnName, VariantValue.Operation? operation = null)
    {
        var columnIndex = this.GetColumnIndexByName(columnName);
        if (columnIndex < 0)
        {
            throw new QueryCatException(
                string.Format(Resources.Errors.CannotFindColumn, columnName));
        }
        var keyValue = FindKeyColumnValue(columnIndex, operation.HasValue ? [operation.Value] : []);
        if (keyValue == null || !keyValue.IsSet)
        {
            var keyColumn = GetKeyColumn(columnIndex, operation);
            if (keyColumn.IsRequired)
            {
                throw new QueryMissedCondition(Columns[keyColumn.ColumnIndex].FullName, keyColumn.GetOperations());
            }
            return VariantValue.Null;
        }
        return keyValue.Value;
    }

    /// <summary>
    /// Try to get key column value by column name.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Operation.</param>
    /// <param name="value">Out value or null.</param>
    /// <returns><c>True</c> if found, <c>false</c> otherwise.</returns>
    public bool TryGetKeyColumnValue(string columnName, VariantValue.Operation? operation, out VariantValue value)
    {
        var columnIndex = this.GetColumnIndexByName(columnName);
        if (columnIndex < 0)
        {
            value = VariantValue.Null;
            return false;
        }
        var keyValue = FindKeyColumnValue(columnIndex, operation.HasValue ? [operation.Value] : []);
        if (keyValue == null || !keyValue.IsSet)
        {
            value = VariantValue.Null;
            return false;
        }
        var keyColumn = GetKeyColumn(columnIndex, operation);
        if (keyColumn.IsRequired)
        {
            throw new QueryMissedCondition(Columns[columnIndex].FullName, keyColumn.GetOperations());
        }
        value = keyValue.Value;
        return !value.IsNull;
    }

    private KeyColumnValue? FindKeyColumnValue(
        int keyColumnIndex,
        params IList<VariantValue.Operation> operations)
    {
        if (!_setKeyColumns.TryGetValue(keyColumnIndex, out var keyColumnValues))
        {
            return null;
        }
        var keyColumnValueResult = Array.Find(keyColumnValues,
            skc => skc.IsSet && skc.KeyColumnIndex == keyColumnIndex
                             && (operations.Count < 1 || operations.Contains(skc.Operation)));
        if (keyColumnValueResult == null)
        {
            return null;
        }
        return keyColumnValueResult;
    }

    protected void AddKeyColumns(IReadOnlyList<KeyColumn> keyColumns)
    {
        var originalLength = _keyColumns.Length;
        Array.Resize(ref _keyColumns, _keyColumns.Length + keyColumns.Count);
        for (var i = 0; i < keyColumns.Count; i++)
        {
            _keyColumns[originalLength + i] = keyColumns[i];
        }
        Array.Sort(_keyColumns, KeyColumnColumnIndexComparer.Instance);
    }

    #endregion

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
