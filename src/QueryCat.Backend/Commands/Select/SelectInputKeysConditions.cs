using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands.Select;

internal record SelectInputKeysConditions(
    IRowsInputKeys RowsInput,
    Column Column,
    KeyColumn KeyColumn,
    SelectQueryCondition[] Conditions
);
