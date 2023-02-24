# Components

There are following main components:

### VariantValue

The `QueryCat.Backend.Types.VariantValue` type is a special type that can contain any kind of data. One of: string, number, object, etc. It is widely used for any value representation within the application. Also, the nullable value is supported.

### Column

The `QueryCat.Backend.Relational.Column` is the relational column representation. It contains name, type and optional description.

### Row

The `QueryCat.Backend.Relational.Row` type contains the array of `VariantValue`. Also, it has a reference to columns schema definition.

Here is a simple schema of rows frame (table).

```
      | Column 1        | Column 2
------|---------------------------------
Row 1 |  VariantValue 1 | VariantValue 2
Row 2 |  VariantValue 3 | VariantValue 4
Row 3 |  VariantValue 5 | VariantValue 6
```

### IRowsInput

The `QueryCat.Backend.Abstractions.IRowsInput` is the input rows source. It can be used in `FROM` SQL clause. This is the low-level abstraction that works on "value" level.

- `Columns`. Array of columns.
- `Open`. Open the rows input and prepare it for reading. After this call the `Columns` property must be initialized.
- `SetContext`. Set query context. See the explanation below.
- `ReadValue`. Read the value at the specified column index.
- `ReadNext`. Go to the next row. Return FALSE if no rows anymore.
- `Close`. Close the rows input and release all the resources.
- `Reset`. Reset current state. Ropen the input.

The QueryCat uses this interface to read various rows sources. It calls the methods to get data. For example, you implemented the rows input that reads following table:

| Id | Name |
| --- | --- |
| 1 | Alice |
| 2 | Bob |
| 3 | Jack |

For query `SELECT * FROM input() WHERE Id > 2` the calling sequence will be like this:

```
Open() -- Open table.
SetContext() -- Set input context.
ReadNext() -- Go to row #1.
ReadValue() -- Column "Id".
ReadNext() -- Go to row #2.
ReadValue() -- Column "Id".
ReadNext() -- Go to row #3.
ReadValue() -- Column "Id".
ReadValue() -- Column "Name".
ReadNext() -- Go to row #4. Returns FALSE since there are no more rows.
Close() -- Close table.
```

### IRowsOutput

The `QueryCat.Backend.Abstractions.IRowsOutput` is the output rows source. It can be used in `INTO` SQL clause.

- `Open`. Open the rows output and prepare it for writing.
- `Write`. Write `Row` instance.
- `Close`. Close the rows output and release all the resources.
- `Reset`. Reset current state. Ropen the output.

### IRowsIterator

The `QueryCat.Backend.Abstractions.IRowsIterator` interface is the iterator pattern implementation for rows. This is high-level abstraction that works on rows level. It can wrap any `IRowsInput`.

- `Columns`. Array of columns.
- `Current`. The current row.
- `MoveNext`. Move to the next row. Return FALSE if no rows available.

Usage example:

```csharp
while (rowsIterator.MoveNext())
{
    Console.WriteLine(rowsIterator.Current);
}
```

### QueryContext

The `QueryCat.Backend.Storage.QueryContext` contains data and methods for extended query processing.

1. Allows to provide key columns. This can be used to improve query caching. Example.

You develop input source for users search endpoint like this `GET /api/users?name=John&status=pending&page=1&pageSize=10`.

The SQL is `select * from ldap_users_source() where name = 'John' and status = 'pending'`.

The first way to implement rows input is to fetch all users and allow QueryCat SQL engine to do the filtering. This is not an optimal way since there can be a lot of users.

As you see, the API endpoint allows restricting search by certain conditions. We can get these key from query context. Use this code to register key column handler.

```csharp
QueryContext.InputInfo.AddKeyColumn(
    "name",
    isRequired: false,
    action: value => _name = value.AsString
);
QueryContext.InputInfo.AddKeyColumn(
    "status",
    operation: VariantValue.Operation.Equals,
    isRequired: false,
    action: value => _status = value.AsString
);
```

The "action" handlers will be executed once meet certain search criteria.

2. Provides simple key-value storage to share information between requests. See `InputConfigStorage`.
