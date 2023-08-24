using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Indexes;

namespace QueryCat.Backend.Commands.Select.Iterators;

internal record WindowFunctionInfo(
    int ColumnIndex,
    IFuncUnit[] PartitionFormatters,
    IFuncUnit[] OrderFunctions,
    OrderColumnData[] Orders,
    IFuncUnit[] AggregateValues,
    AggregateTarget AggregateTarget);
