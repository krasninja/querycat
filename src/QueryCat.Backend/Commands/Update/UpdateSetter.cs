using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Commands.Update;

internal record UpdateSetter(int ColumnIndex, IFuncUnit FuncUnit);
