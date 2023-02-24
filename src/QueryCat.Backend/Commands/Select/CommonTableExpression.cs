using QueryCat.Backend.Abstractions;

namespace QueryCat.Backend.Commands.Select;

internal record struct CommonTableExpression(string Name, IRowsIterator RowsIterator);
