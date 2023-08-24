using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Update;

internal sealed class UpdateCommandHandler : CommandHandler
{
    private readonly SelectCommandContext _selectCommandContext;
    private readonly UpdateSetter[] _setters;
    private readonly IRowsInputUpdate _rowsInput;

    public UpdateCommandHandler(
        SelectCommandContext selectCommandContext,
        UpdateSetter[] setters)
    {
        _selectCommandContext = selectCommandContext;
        _setters = setters;

        if (selectCommandContext.RowsInputIterator?.RowsInput is not IRowsInputUpdate rowsInputUpdate)
        {
            throw new ArgumentException("Rows input must be updatable.", nameof(selectCommandContext));
        }
        _rowsInput = rowsInputUpdate;
    }

    /// <inheritdoc />
    public override VariantValue Invoke()
    {
        var updateCount = 0;
        while (_selectCommandContext.CurrentIterator.MoveNext())
        {
            updateCount++;
            for (var i = 0; i < _setters.Length; i++)
            {
                var value = _setters[i].FuncUnit.Invoke();
                _rowsInput.UpdateValue(_setters[i].ColumnIndex, value);
            }
        }

        return new VariantValue(updateCount);
    }
}
