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
    private readonly List<KeyColumn> _keyColumns = new();

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

    private readonly List<KeyColumnValue> _setKeyColumns = [];

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

    private KeyColumn GetKeyColumn(int columnIndex)
    {
        var keyColumn = _keyColumns.Find(kc => kc.ColumnIndex == columnIndex);
        if (keyColumn == null)
        {
            throw new InvalidOperationException($"The column with index '{columnIndex}' is not a key column.");
        }
        return keyColumn;
    }

    private KeyColumnValue GetOrCreateKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        var keyColumn = GetKeyColumn(columnIndex);
        if (!keyColumn.ContainsOperation(operation))
        {
            throw new InvalidOperationException($"The operation '{operation}' is not supported by the key column index '{keyColumn.ColumnIndex}'.");
        }
        var kcv = _setKeyColumns.Find(kc => kc.KeyColumnIndex == columnIndex && kc.Operation == operation);
        if (kcv == null)
        {
            kcv = new KeyColumnValue(columnIndex);
            _setKeyColumns.Add(kcv);
        }
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
        var keyValue = GetKeyColumn(columnIndex, operation.HasValue ? [operation.Value] : []);
        if (keyValue == null || !keyValue.IsSet)
        {
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
        var keyValue = GetKeyColumn(columnIndex, operation.HasValue ? [operation.Value] : []);
        if (keyValue == null || !keyValue.IsSet)
        {
            value = VariantValue.Null;
            return false;
        }
        var keyColumn = GetKeyColumn(columnIndex);
        if (keyColumn.IsRequired)
        {
            throw new QueryMissedCondition(Columns[columnIndex].FullName, keyColumn.GetOperations());
        }
        value = keyValue.Value;
        return !value.IsNull;
    }

    private KeyColumnValue? GetKeyColumn(
        int keyColumnIndex,
        params IList<VariantValue.Operation> operations)
    {
        var keyColumnValueResult = _setKeyColumns
            .Find(skc => skc.IsSet && skc.KeyColumnIndex == keyColumnIndex
                                   && (operations.Count < 1 || operations.Contains(skc.Operation)));
        if (keyColumnValueResult == null)
        {
            return null;
        }
        var keyColumn = GetKeyColumn(keyColumnValueResult.KeyColumnIndex);
        if (keyColumnValueResult.Value.IsNull && keyColumn.IsRequired)
        {
            throw new QueryMissedCondition(
                Columns[keyColumnValueResult.KeyColumnIndex].FullName,
                keyColumn.GetOperations());
        }
        return keyColumnValueResult;
    }

    protected void AddKeyColumns(IReadOnlyCollection<KeyColumn> keyColumns) => _keyColumns.AddRange(keyColumns);

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
