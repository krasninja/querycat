using QueryCat.Backend.Core.Functions;

namespace QueryCat.Backend.Commands.Update;

public record UpdateSetter(int ColumnIndex, IFuncUnit FuncUnit);
