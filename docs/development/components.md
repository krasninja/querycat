# Components

There are following main components:

### VariantValue

The `QueryCat.Backend.Core.Types.VariantValue` type is a special type that can contain any kind of data. One of: string, number, object, etc. It is widely used for any value representation within the application. Is also can be null instead of storing any value.

### Column

The `QueryCat.Backend.Core.Relational.Column` is the relational column representation. It contains name, data type and optional description.

### Row

The `QueryCat.Backend.Core.Relational.Row` type contains the array of `VariantValue`. Also, it has a reference to columns schema definition.

Here is a simple schema of rows frame (table).

```
      | Column 1        | Column 2
------|---------------------------------
Row 1 |  VariantValue 1 | VariantValue 2
Row 2 |  VariantValue 3 | VariantValue 4
Row 3 |  VariantValue 5 | VariantValue 6
```

### IRowsInput

The `QueryCat.Backend.Core.Data.IRowsInput` is the input rows source. It can be used in `FROM` SQL clause. This is the low-level abstraction that works on "value" level.

- `Columns`. Array of columns.
- `OpenAsync`. Open the rows input and prepare it for reading. After this call the `Columns` property must be initialized.
- `QueryContext`. Set query context. See the explanation below.
- `ReadValue`. Read the value at the specified column index.
- `ReadNextAsync`. Go to the next row. Return FALSE if no rows anymore.
- `CloseAsync`. Close the rows input and release all the resources.
- `ResetAsync`. Reset current state. Reopen the input.

The QueryCat uses this interface to read various rows sources. It calls the methods to get data. For example, you implement the rows input that reads the following table:

| Id  | Name  |
| --- | ---   |
| 1   | Alice |
| 2   | Bob   |
| 3   | Jack  |

For query `SELECT * FROM input() WHERE Id > 2` the calling sequence will be like this:

```
OpenAsync() -- Open table.
QueryContext -- Set input context property.
ReadNextAsync() -- Go to row #1.
ReadValue() -- Column "Id".
ReadNextAsync() -- Go to row #2.
ReadValue() -- Column "Id".
ReadNextAsync() -- Go to row #3.
ReadValue() -- Column "Id".
ReadValue() -- Column "Name".
ReadNextAsync() -- Go to row #4. Returns FALSE since there are no more rows.
CloseAsync() -- Close table.
```

### IRowsOutput

The `QueryCat.Backend.Core.Data.IRowsOutput` is the output rows source. It can be used in `INTO` SQL clause.

- `OpenAsync`. Open the rows output and prepare it for writing.
- `WriteValuesAsync`. Write `Row` instance.
- `CloseAsync`. Close the rows output and release all the resources.
- `ResetAsync`. Reset current state. Reopen the output.

### IRowsIterator

The `QueryCat.Backend.Core.Data.IRowsIterator` interface is the iterator pattern implementation for rows. This is high-level abstraction that works on rows level. It can wrap any `IRowsInput`.

- `Columns`. Array of columns.
- `Current`. The current row.
- `MoveNextAsync`. Move to the next row. Return FALSE if no rows available.

Usage example:

```csharp
while (await rowsIterator.MoveNextAsync())
{
    Console.WriteLine(rowsIterator.Current);
}
```

### QueryContext

The `QueryCat.Backend.Core.Data.QueryContext` contains data and methods for extended query processing.

1. Provides general query information: selected columns, offset, limit. See `QueryInfo` property.

2. Provides simple key-value storage to share information between requests. See `InputConfigStorage`.

## IRowsInputKeys

The interfaces declare the additional methods to describe keys (index) columns. The key column is the special column that can be used for data filter. Before first `ReadNextAsync` calling, the QueryCat host calls `SetKeyColumnValue` method. That way, you can optimize your select strategy. For example, instead of parsing all the files, parse the ones that match index.

- `GetKeyColumns`. Get all supported keys columns.
- `SetKeyColumnValue`. Set key column value before query.
- `UnsetKeyColumnValue`. Clear the value for the specified key column.
