using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands.Select;

internal record SelectInputKeysConditions(
    IRowsInput RowsInput,
    Column Column,
    KeyColumn KeyColumn,
    SelectQueryCondition[] Conditions
);
