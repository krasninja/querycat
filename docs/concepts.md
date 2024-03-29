# Concepts

In general QueryCat is made up of three components:

- **Rows inputs** are data providers that are equivalent to rows in a SQL table containing the data you want to process. There are built-in Rows Inputs (CSV, IIS W3C, others) but you can attach more using plugin DLLs.

- **SQL-Like Engine** processes the rows generated by a Rows Input, using a dialect of the SQL language that includes common SQL clauses (WITH, SELECT, UNION, WHERE, GROUP BY, HAVING, ORDER BY), aggregate functions (SUM, COUNT, AVG, MAX, MIN), and a set of functions. The resulting records are then sent to an Rows Output.

- **Rows Output** are generic consumers of records; they can be thought of as SQL tables that receive the results of the data processing.
QueryCats's built-in outputs can be CSV file, system console and others.

```
Rows Input -> SQL backend -> Rows Output
```

## Types

There are following column data types are supported:

| Name | Aliases | Range | Comment |
| --- | --- | --- | --- |
| INTEGER | INT, INT8 | -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 | |
| FLOAT | REAL | ±5.0 × 10−324 to ±1.7 × 10308 | Precision is ~15-17 digits. |
| NUMERIC | DECIMAL | ±1.0 x 10-28 to ±7.9228 x 1028 | Precision is 28-29 digits. |
| BOOLEAN | BOOL | `true` or `false` | |
| TIMESTAMP | | Date and time values. |
| STRING | TEXT | Text data. |
| INTERVAL | | | Time interval. |
| BLOB | | | Binary large object. |
| OBJECT | | |
| NULL | | | Special type that represents "nothing" value. |

* For boolean `true` additional values can be used: `1`, `yes`, `on`, `t`. Boolean `false`: `0`, `no`, `off`, `f`.

* Every column is considered to be nullable. If value is empty it will be parsed as NULL.

* You can unescape string by preceding 'E' character (upper or lower case) before string. For example: `E'Hello,\n\nHow are you?'`.
