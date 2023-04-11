using QueryCat.Backend.Functions;

namespace QueryCat.Backend.Commands.Update;

public record UpdateSetter(int ColumnIndex, IFuncUnit FuncUnit);
