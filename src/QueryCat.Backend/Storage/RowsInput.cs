using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The class simplifies <see cref="IRowsInput" /> interface implementation.
/// </summary>
public abstract class RowsInput : IRowsInputKeys
{
    private bool _isFirstCall = true;
    private readonly List<KeyColumn> _keyColumns = new();
    private readonly Dictionary<string, Action<VariantValue>> _initKeysColumnsCallbacks = new();

    /// <summary>
    /// Query context.
    /// </summary>
    public QueryContext QueryContext { get; set; } = new EmptyQueryContext();

    /// <inheritdoc />
    public abstract Column[] Columns { get; protected set; }

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
    public virtual void SetKeyColumnValue(string columnName, VariantValue value)
    {
        if (_initKeysColumnsCallbacks.TryGetValue(columnName, out var action))
        {
            action.Invoke(value);
        }
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
        _keyColumns.Add(new KeyColumn(columnName, isRequired, VariantValue.Operation.Equals));
        if (set != null)
        {
            _initKeysColumnsCallbacks[columnName] = set;
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
        _keyColumns.Add(new KeyColumn(columnName, isRequired, operation));
        if (set != null)
        {
            _initKeysColumnsCallbacks[columnName] = set;
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
        _keyColumns.Add(new KeyColumn(columnName, isRequired, operation, orOperation));
        if (set != null)
        {
            _initKeysColumnsCallbacks[columnName] = set;
        }
    }

    #endregion
}
