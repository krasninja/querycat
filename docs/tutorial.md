# Tutorial

The tutorial walks through example CSV file queries. Let's prepare the example data first:

```
$ seq 1 1000 > example.csv
```

## Simple Querying

Query example CSV file with simple aggregate functions and filtering:

```
$ qcat "SELECT sum(column0) as s, count(*) AS total FROM './example.csv' WHERE column0 % 2 = 0"

| s       | total |
| ------- | ----- |
| 250500  | 500   |
```

*Tip: You can also pass file name as command line argument: `qcat --var f=/tmp/example.csv "SELECT * FROM f"`.*

As you can see, you can specify file name in `FROM` clause. The QueryCat tries to understand file format by file extension. Also, if columns names are not specified, the default columns names are set (like "column1" in example). QueryCat has simple analyzer to understand whether file has header row or not. But if it doesn't work correctly, you can set it directly within `FORMAT` clause:

```
$ qcat "SELECT avg([1]) AS avg FROM './example.csv' FORMAT csv(has_header=>true)"

| total  |
| ------ |
| 501.00 |
```

In the query above, the `csv(has_header=>true)` is the function call with passing the one argument value. You should use `=>` operator if you want to specify argument name (like `has_header`). You can achieve the same effect with calling it like this: `csv(true)`. Also, since we have the header now, we can reference the only column with `[1]`. You can wrap a column name within `[]` block if it has special characters or starts with a digit.

The `FORMAT` clause requires you to specify a function that returns any formatter for the file. It can be `csv`, `tsv`, `iisw3c` or others.

## Grouping, Limiting

```
$ qcat "SELECT column0 % 3 FROM './example.csv' GROUP BY column0 % 3 HAVING column0 % 3 > 0"
1
2
```

Since there is only one column (without name) the QueryCat produces compact output.

You can use `GROUP BY` and `HAVING` clauses for grouping. To limit query result, you should use `OFFEST` and `FETCH` clauses:

```
$ qcat "SELECT * FROM './example.csv' OFFSET 4 FETCH 5"

| filename                 | column0 |
| ------------------------ | ------- |
| /home/ivan/1/example.csv | 5       |
| /home/ivan/1/example.csv | 6       |
| /home/ivan/1/example.csv | 7       |
| /home/ivan/1/example.csv | 8       |
| /home/ivan/1/example.csv | 9       |
```

## Ordering

Ordering is implemented as part of SQL standard.

```
qcat "SELECT * FROM './example.csv' ORDER BY column0 DESC FETCH 3"

| filename                 | column0 |
| ------------------------ | ------- |
| /home/ivan/1/example.csv | 1000    |
| /home/ivan/1/example.csv | 999     |
| /home/ivan/1/example.csv | 998     |
```

## Schema

For every row's source (files, logs, web resources, etc) QueryCat tries to make a schema: determine column names and their types. It really helps to make correct queries. For example, it doesn't make sense to filter or order by integer column if it is represented as a string. There are the following main column types: integer, string, float, timestamp (date and time), boolean and numeric. QueryCat can show you the used schema:

```
$ qcat schema "SELECT * FROM './example.csv'"

| name                      | type       | description |
| ------------------------- | ---------- | ----------- |
| filename                  | String     | File path.  |
| column0                   | Integer    |             |
```
