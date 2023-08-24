using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands.Select;

internal record struct CommonTableExpression(string Name, IRowsIterator RowsIterator);
