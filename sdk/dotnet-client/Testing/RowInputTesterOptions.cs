using System;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Client.Testing;

/// <summary>
/// Run options for <see cref="RowInputTester" />.
/// </summary>
public sealed class RowInputTesterOptions
{
    private readonly RowInputTester _tester;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="tester">Tester.</param>
    internal RowInputTesterOptions(RowInputTester tester)
    {
        _tester = tester;
    }

    /// <summary>
    /// Max rows to fetch.
    /// </summary>
    public int? MaxRowsCount = null;

    /// <summary>
    /// Set column condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="value">Value.</param>
    /// <param name="operation">Operation.</param>
    /// <returns>Instance of <see cref="RowInputTester" />.</returns>
    public RowInputTesterOptions SetConditionValue(string columnName, VariantValue value,
        VariantValue.Operation? operation = null)
    {
        var columnIndex = _tester.RowsInput.GetColumnIndexByName(columnName);
        if (columnIndex < 0)
        {
            throw new QueryCatException($"Cannot find column '{columnName}'.");
        }

        if (_tester.RowsInput is not KeysRowsInput enumerableRowsInput)
        {
            throw new InvalidOperationException("Invalid rows input type.");
        }

        enumerableRowsInput.SetKeyColumnValue(columnIndex, value, operation ?? VariantValue.Operation.Equals);
        return this;
    }

    /// <summary>
    /// Set column condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="value">Value.</param>
    /// <param name="operation">Operation.</param>
    /// <returns>Instance of <see cref="RowInputTester" />.</returns>
    public RowInputTesterOptions SetConditionValue(string columnName, object value,
        VariantValue.Operation? operation = null)
        => SetConditionValue(columnName, VariantValue.CreateFromObject(value), operation);
}
