using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

internal readonly record struct CommonTableExpression(string Name, IRowsIterator RowsIterator)
{
    public IRowsInput RowsInputProxy { get; } = new RowsIteratorInput(RowsIterator);
}
