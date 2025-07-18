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

    [DebuggerDisplay("{KeyColumn} => {Value}")]
    private sealed class KeyColumnValue(KeyColumn keyColumn, VariantValue value = default)
    {
        public KeyColumn KeyColumn { get; } = keyColumn;

        public VariantValue Value { get; set; } = value;

        public bool IsSet { get; set; } = true;
    }

    private KeyColumnValue[][] _setKeyColumns = [];

    /// <summary>
    /// Is <c>true</c> if user set all key columns in his query.
    /// </summary>
    public bool AreAllKeyColumnsSet => _keyColumns.Count == _setKeyColumns.Length;

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = [];

    protected virtual void InitializeKeyColumns(bool force = false)
    {
        if (_setKeyColumns.Length == Columns.Length && !force)
        {
            return;
        }

        _setKeyColumns = new KeyColumnValue[Columns.Length][];
        for (var i = 0; i < Columns.Length; i++)
        {
            _setKeyColumns[i] = _keyColumns
                .Where(kc => kc.ColumnIndex == i)
                .Select(kc => new KeyColumnValue(kc))
                .ToArray();
        }
    }

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
        _setKeyColumns = [];
        InitializeKeyColumns();
        await CloseAsync(cancellationToken);
        await base.ResetAsync(cancellationToken);
    }

    #region IRowsInputKeys

    /// <inheritdoc />
    public override IReadOnlyList<KeyColumn> GetKeyColumns() => _keyColumns;

    /// <inheritdoc />
    public override void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        InitializeKeyColumns();
        var kcv = _setKeyColumns[columnIndex].First(kc => kc.KeyColumn.ContainsOperation(operation));
        kcv.Value = value;
        kcv.IsSet = true;
    }

    /// <inheritdoc />
    public override void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
        InitializeKeyColumns();
        var kcv = _setKeyColumns[columnIndex].First(kc => kc.KeyColumn.ContainsOperation(operation));
        kcv.Value = VariantValue.Null;
        kcv.IsSet = false;
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
        var keyValue = GetKeyColumn(_setKeyColumns[columnIndex], operation);
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
        var keyValue = GetKeyColumn(_setKeyColumns[columnIndex], operation);
        if (keyValue == null || !keyValue.IsSet)
        {
            value = VariantValue.Null;
            return false;
        }
        if (keyValue.KeyColumn.IsRequired)
        {
            throw new QueryMissedCondition(Columns[columnIndex].FullName, keyValue.KeyColumn.GetOperations());
        }
        value = keyValue.Value;
        return !value.IsNull;
    }

    private KeyColumnValue? GetKeyColumn(
        KeyColumnValue[] keyColumnValues,
        VariantValue.Operation? operation = null,
        VariantValue.Operation? orOperation = null)
    {
        var keyColumnValueResult = keyColumnValues.FirstOrDefault(c =>
            (operation == null || c.KeyColumn.ContainsOperation(operation.Value))
            && (orOperation == null || c.KeyColumn.ContainsOperation(orOperation.Value)));
        if (keyColumnValueResult == null || !keyColumnValueResult.IsSet)
        {
            return null;
        }
        if (keyColumnValueResult.Value.IsNull && keyColumnValueResult.KeyColumn.IsRequired)
        {
            throw new QueryMissedCondition(
                Columns[keyColumnValueResult.KeyColumn.ColumnIndex].FullName,
                keyColumnValueResult.KeyColumn.GetOperations());
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
