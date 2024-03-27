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

    private sealed class KeyColumnValue
    {
        public KeyColumn KeyColumn { get; }

        public VariantValue Value { get; set; }

        public KeyColumnValue(KeyColumn keyColumn, VariantValue value = default)
        {
            KeyColumn = keyColumn;
            Value = value;
        }
    }

    private KeyColumnValue[][] _setKeyColumns = Array.Empty<KeyColumnValue[]>();

    protected IEnumerator<TClass>? Enumerator { get; set; }

    /// <summary>
    /// Is <c>true</c> if user set all key columns in his query.
    /// </summary>
    public bool AreAllKeyColumnsSet => _keyColumns.Count == _setKeyColumns.Length;

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

    private void InitializeKeyColumns(bool force = false)
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
        _setKeyColumns = Array.Empty<KeyColumnValue[]>();
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
        InitializeKeyColumns();
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
    public virtual void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
        InitializeKeyColumns();
        var kcv = _setKeyColumns[columnIndex].First(kc => kc.KeyColumn.ContainsOperation(operation));
        kcv.Value = value;
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
        var keyValue = GetKeyColumn(_setKeyColumns[columnIndex], operation);
        return keyValue?.Value ?? VariantValue.Null;
    }

    /// <summary>
    /// Try get key column value by column name.
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
        if (keyValue == null)
        {
            value = VariantValue.Null;
            return false;
        }
        value = keyValue.Value;
        return true;
    }

    private KeyColumnValue? GetKeyColumn(
        KeyColumnValue[] keyColumnValues,
        VariantValue.Operation? operation = null,
        VariantValue.Operation? orOperation = null)
    {
        return keyColumnValues.FirstOrDefault(c =>
            (operation == null || c.KeyColumn.ContainsOperation(operation.Value))
            && (orOperation == null || c.KeyColumn.ContainsOperation(orOperation.Value)));
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
