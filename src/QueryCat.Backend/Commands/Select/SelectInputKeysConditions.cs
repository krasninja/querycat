using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Commands.Select;

internal record SelectInputKeysConditions(
    IRowsInputKeys RowsInput,
    Column Column,
    KeyColumn KeyColumn,
    SelectQueryCondition[] Conditions
);
