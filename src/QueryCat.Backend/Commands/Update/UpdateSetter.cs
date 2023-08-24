using QueryCat.Backend.Abstractions.Functions;

namespace QueryCat.Backend.Commands.Update;

public record UpdateSetter(int ColumnIndex, IFuncUnit FuncUnit);
