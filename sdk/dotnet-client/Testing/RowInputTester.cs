using System;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Client.Testing;

/// <summary>
/// Simple output and tester for <see cref="IRowsInput" />.
/// </summary>
public class RowInputTester
{
    private readonly IRowsInput _rowsInput;

    /// <summary>
    /// Rows input.
    /// </summary>
    public IRowsInput RowsInput => _rowsInput;

    /// <summary>
    /// Test execution thread.
    /// </summary>
    public IExecutionThread ExecutionThread { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsInput">Target rows input.</param>
    public RowInputTester(IRowsInput rowsInput)
    {
        ExecutionThread = new TestExecutionThread();
        _rowsInput = rowsInput;
        _rowsInput.QueryContext = new PluginQueryContext(
            new QueryContextQueryInfo(_rowsInput.Columns),
            ExecutionThread.ConfigStorage);
    }

    public FunctionCallInfo CreateFunctionCallInfo(params VariantValue[] args)
    {
        var functionCallInfo = new FunctionCallInfo(ExecutionThread, args);
        return functionCallInfo;
    }

    /// <summary>
    /// Set column condition.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="value">Value.</param>
    /// <param name="operation">Operation.</param>
    /// <returns>Instance of <see cref="RowInputTester" />.</returns>
    public RowInputTester SetConditionValue(string columnName, VariantValue value, VariantValue.Operation? operation = null)
    {
        var columnIndex = _rowsInput.GetColumnIndexByName(columnName);
        if (columnIndex < 0)
        {
            throw new QueryCatException($"Cannot find column '{columnName}'.");
        }
        if (_rowsInput is not KeysRowsInput enumerableRowsInput)
        {
            throw new InvalidOperationException("Invalid rows input type.");
        }
        enumerableRowsInput.SetKeyColumnValue(columnIndex, value, operation ?? VariantValue.Operation.Equals);
        return this;
    }

    /// <summary>
    /// Run the query.
    /// </summary>
    /// <param name="optionsAction">Options.</param>
    public virtual void Run(Action<RowInputTesterOptions> optionsAction)
    {
        var options = new RowInputTesterOptions(this);
        _rowsInput.Open();
        optionsAction.Invoke(options);

        try
        {
            var rowIndex = 0;
            while (RowsInput.ReadNext() && (options.MaxRowsCount.HasValue && options.MaxRowsCount.Value > rowIndex))
            {
                Console.WriteLine($"-[ ROW {rowIndex} ]------");
                for (var columnIndex = 0; columnIndex < _rowsInput.Columns.Length; columnIndex++)
                {
                    RowsInput.ReadValue(columnIndex, out var value);
                    Console.WriteLine($"{RowsInput.Columns[columnIndex].FullName}: " + value);
                }
                rowIndex++;
            }
        }
        finally
        {
            RowsInput.Close();
            CleanKeysValues();
        }
    }

    private void CleanKeysValues()
    {
        if (_rowsInput is IRowsInputKeys keys)
        {
            foreach (var keyColumn in keys.GetKeyColumns())
            {
                keys.SetKeyColumnValue(keyColumn.ColumnIndex, VariantValue.Null, keyColumn.Operation1);
                if (keyColumn.Operation2 != null)
                {
                    keys.SetKeyColumnValue(keyColumn.ColumnIndex, VariantValue.Null, keyColumn.Operation2.Value);
                }
            }
        }
    }
}
