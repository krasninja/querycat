using QueryCat.Backend.Functions;
using QueryCat.Backend.Indexes;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Commands.Select.Iterators;

internal record OrderByData(IFuncUnit Func, OrderDirection Direction, NullOrder NullOrder);
