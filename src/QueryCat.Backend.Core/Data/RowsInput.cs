using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// The class simplifies <see cref="IRowsInput" /> interface implementation.
/// </summary>
public abstract class RowsInput : IRowsInputKeys
{
    private sealed class KeyColumnData
    {
        public VariantValue Value { get; set; } = VariantValue.Null;

        public Action<VariantValue>? Callback { get; set; }
    }

    private bool _isFirstCall = true;
    private readonly List<KeyColumn> _keyColumns = new();
    private readonly Dictionary<KeyColumn, KeyColumnData> _setKeyColumns = new();

    /// <summary>
    /// Query context.
    /// </summary>
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    /// <summary>
    /// Is <c>true</c> if user set all key columns in his query.
    /// </summary>
    public bool AreAllKeyColumnsSet => _keyColumns.Count == _setKeyColumns.Count;

    /// <inheritdoc />
    public abstract Column[] Columns { get; protected set; }

    /// <inheritdoc />
    public virtual string[] UniqueKey { get; protected set; } = Array.Empty<string>();

    /// <inheritdoc />
    public abstract void Open();

    /// <inheritdoc />
    public abstract void Close();

    /// <inheritdoc />
    public abstract ErrorCode ReadValue(int columnIndex, out VariantValue value);

    /// <inheritdoc />
    public virtual bool ReadNext()
    {
        if (_isFirstCall)
        {
            Load();
            _isFirstCall = false;
        }
        return true;
    }

    /// <inheritdoc />
    public virtual void Reset()
    {
        _isFirstCall = true;
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
    }

    /// <summary>
    /// The method is called before first ReadNext to initialize input.
    /// </summary>
    protected virtual void Load()
    {
    }

    #region IRowsInputKeys

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => _keyColumns;

    /// <inheritdoc />
    public virtual void SetKeyColumnValue(string columnName, VariantValue value, VariantValue.Operation operation)
    {
        var data = GetKeyColumnData(columnName, operation);
        if (data == null)
        {
            return;
        }
        data.Value = value;
        data.Callback?.Invoke(data.Value);
    }

    #endregion

    #region Key columns

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="isRequired">Is this the required condition.</param>
    /// <param name="set">Initialization action.</param>
    public void AddKeyColumn(
        string columnName,
        bool isRequired = false,
        Action<VariantValue>? set = null)
    {
        var keyColumn = new KeyColumn(columnName, isRequired, VariantValue.Operation.Equals);
        _keyColumns.Add(keyColumn);
        _setKeyColumns[keyColumn] = new KeyColumnData();
        if (set != null)
        {
            _setKeyColumns[keyColumn].Callback = set;
        }
    }

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Key operation.</param>
    /// <param name="isRequired">Is this the required condition.</param>
    /// <param name="set">Initialization action.</param>
    public void AddKeyColumn(
        string columnName,
        VariantValue.Operation operation,
        bool isRequired = false,
        Action<VariantValue>? set = null)
    {
        var keyColumn = new KeyColumn(columnName, isRequired, operation);
        _keyColumns.Add(keyColumn);
        _setKeyColumns[keyColumn] = new KeyColumnData();
        if (set != null)
        {
            _setKeyColumns[keyColumn].Callback = set;
        }
    }

    /// <summary>
    /// Add key column information.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Key operation.</param>
    /// <param name="orOperation">Alternate key operation.</param>
    /// <param name="isRequired">Is this the required condition.</param>
    /// <param name="set">Initialization action.</param>
    public void AddKeyColumn(
        string columnName,
        VariantValue.Operation operation,
        VariantValue.Operation orOperation,
        bool isRequired = false,
        Action<VariantValue>? set = null)
    {
        var keyColumn = new KeyColumn(columnName, isRequired, operation, orOperation);
        _keyColumns.Add(keyColumn);
        _setKeyColumns[keyColumn] = new KeyColumnData();
        if (set != null)
        {
            _setKeyColumns[keyColumn].Callback = set;
        }
    }

    /// <summary>
    /// Get key column value by column name.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Operation.</param>
    /// <returns>Value or null.</returns>
    public VariantValue GetKeyColumnValue(string columnName, VariantValue.Operation operation)
    {
        var data = GetKeyColumnData(columnName, operation);
        if (data != null)
        {
            return data.Value;
        }
        return VariantValue.Null;
    }

    private KeyColumnData? GetKeyColumnData(
        string columnName,
        VariantValue.Operation operation = VariantValue.Operation.Equals,
        VariantValue.Operation? orOperation = null)
    {
        var keyColumn = _keyColumns.Find(c =>
            c.Operations[0] == operation
            && (orOperation == null || c.Operations[0] == orOperation)
            && Column.NameEquals(c.ColumnName, columnName));
        if (keyColumn == null)
        {
            throw new QueryCatException($"The column '{columnName}' is not found among key columns.");
        }
        if (_setKeyColumns.TryGetValue(keyColumn, out var data))
        {
            return data;
        }
        return null;
    }

    #endregion
}
