using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Update;

internal sealed class UpdateCommandHandler : IFuncUnit
{
    private readonly SelectCommandContext _selectCommandContext;
    private readonly UpdateSetter[] _setters;
    private readonly IRowsInputUpdate _rowsInput;

    /// <inheritdoc />
    public DataType OutputType => DataType.Null;

    public UpdateCommandHandler(
        SelectCommandContext selectCommandContext,
        UpdateSetter[] setters)
    {
        _selectCommandContext = selectCommandContext;
        _setters = setters;

        if (selectCommandContext.RowsInputIterator?.RowsInput is not IRowsInputUpdate rowsInputUpdate)
        {
            throw new ArgumentException(Resources.Errors.RowsInputNotUpdatable, nameof(selectCommandContext));
        }
        _rowsInput = rowsInputUpdate;
    }

    /// <inheritdoc />
    public VariantValue Invoke(IExecutionThread thread)
    {
        var updateCount = 0;
        while (_selectCommandContext.CurrentIterator.MoveNext())
        {
            updateCount++;
            foreach (var setter in _setters)
            {
                var value = setter.FuncUnit.Invoke(thread);
                _rowsInput.UpdateValue(setter.ColumnIndex, value);
            }
        }

        return new VariantValue(updateCount);
    }
}
