using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Implements <see cref="IRowsInput" /> from enumerable.
/// </summary>
/// <typeparam name="TClass">Base enumerable class.</typeparam>
public class EnumerableRowsInput<TClass> : RowsInput, IRowsInputKeys, IDisposable where TClass : class
{
    private readonly ClassRowsFrameBuilder<TClass> _builder = new();

    private readonly List<KeyColumn> _keyColumns = new();
    private readonly Dictionary<KeyColumn, VariantValue> _setKeyColumns = new();

    protected IEnumerator<TClass>? Enumerator { get; set; }

    /// <summary>
    /// Is <c>true</c> if user set all key columns in his query.
    /// </summary>
    public bool AreAllKeyColumnsSet => _keyColumns.Count == _setKeyColumns.Count;

    protected ClassRowsFrameBuilder<TClass> Builder => _builder;

    /// <inheritdoc />
    public override Column[] Columns { get; protected set; } = Array.Empty<Column>();

    public EnumerableRowsInput(IEnumerable<TClass> enumerable, Action<ClassRowsFrameBuilder<TClass>>? setup = null)
    {
        if (setup != null)
        {
            setup.Invoke(_builder);
            // ReSharper disable once VirtualMemberCallInConstructor
            Columns = _builder.Columns.ToArray();
            AddKeyColumns(_builder.KeyColumns);
        }

        Enumerator = enumerable.GetEnumerator();
    }

    /// <inheritdoc />
    public override void Open()
    {
    }

    /// <inheritdoc />
    public override void Close()
    {
        Enumerator?.Dispose();
    }

    /// <inheritdoc />
    public override void Reset()
    {
        Close();
        base.Reset();
    }

    /// <inheritdoc />
    public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        if (Enumerator == null)
        {
            value = VariantValue.Null;
            return ErrorCode.Error;
        }

        value = _builder.GetValue(columnIndex, Enumerator.Current);
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public override bool ReadNext()
    {
        base.ReadNext();
        if (Enumerator == null)
        {
            return false;
        }
        return Enumerator.MoveNext();
    }

    #region IRowsInputKeys

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => _keyColumns;

    /// <inheritdoc />
    public virtual void SetKeyColumnValue(string columnName, VariantValue value, VariantValue.Operation operation)
    {
        var keyColumn = GetKeyColumnData(columnName, operation);
        _setKeyColumns[keyColumn] = value;
    }

    #endregion

    #region Key columns

    /// <summary>
    /// Get key column value by column name.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="operation">Operation.</param>
    /// <returns>Value or null.</returns>
    public VariantValue GetKeyColumnValue(string columnName, VariantValue.Operation operation = VariantValue.Operation.Equals)
    {
        var keyColumn = GetKeyColumnData(columnName, operation);
        return _setKeyColumns.GetValueOrDefault(keyColumn, VariantValue.Null);
    }

    private KeyColumn GetKeyColumnData(
        string columnName,
        VariantValue.Operation? operation = null,
        VariantValue.Operation? orOperation = null)
    {
        var keyColumn = _keyColumns.Find(c =>
            (operation == null || c.ContainsOperation(operation.Value))
            && (orOperation == null || c.ContainsOperation(orOperation.Value))
            && Column.NameEquals(c.ColumnName, columnName));
        if (keyColumn == null)
        {
            throw new QueryCatException(string.Format(Resources.Errors.CannotFindColumn, columnName));
        }
        return keyColumn;
    }

    protected void AddKeyColumns(IReadOnlyCollection<KeyColumn> keyColumns) => _keyColumns.AddRange(keyColumns);

    #endregion

    #region Dispose

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Enumerator?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
