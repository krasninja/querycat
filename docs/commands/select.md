# SELECT

Executes the SQL query against input. If not output is specified (with `INTO` clause) the system console will be used.

## Syntax

```
SELECT [ DISTINCT ] [ TOP number ]
    [ * | expression AS [ alias ] | column AS [ alias ] [, ...] ]
[ INTO [ function : IRowsOutput ] ]
[ FROM [ function : IRowsInput | uri ] FORMAT [ function : IRowsFormatter ] ]
[ WHERE search_condition [, ...] ]
[ GROUP BY [ expression ] [, ...] ]
[ HAVING aggregate_search_condition [, ...] ]
[ ORDER BY [ expression [ ASC | DESC ] ] ]
[ LIMIT number ]
[ OFFSET number [ ROW | ROWS ] ]
[ FETCH [ FIRST | NEXT ] number [ ROW | ROWS ] [ ONLY ]
```

## SELECT

The SELECT clause specifies the fields of the output records to be returned. Also, it supports expressions and can be used for simple calculations. Example:

```sql
SELECT (2 - 1 + 4 * 6) / 3.0
```

The non-standard T-SQL `TOP` clause is supported as well to limit result data set.

## INTO

The INTO clause specifies the custom output target. 

## FROM

The FROM clause specified the input format source(-s). The next expression must be function call that returns rows set. Example:

```sql
SELECT * FROM curl('https://tinyurl.com/24buj7mb')
```

If column contains parenthesis or other special symbols, you can wrap it within square brackets `[]`:

```sql
SELECT [cs(User-Agent)] FROM read_file('u_ex220826.log', formattre=>iisw3c());
```

Also, you can use `FROM` syntax like this:

```sql
SELECT * FROM '/home/ivan/1/example.csv'
```

The URI can be:

- CSV, TSV or other file on local disk (like `/home/ivan/1/example.csv` or `D:\1\example.csv`). The formatter will be resolved by file extension. The path can contain a combination of valid literal path and wildcard (`*` and `?`) characters, but it doesn't support regular expressions.
- Web resource (`https://tinyurl.com/24buj7mb`). The formatter will be resolved by content type.

## GROUP BY

The GROUP BY clause specifies the groups into which output rows are to be placed and, if aggregate functions are included in the SELECT or HAVING clauses, calculates the aggregate functions values for each group. For example, next command aggregates data by states and calculates average, minimum and maximum population values for period 1950-2020:

```sql
SELECT state, avg(population) as 'avg', max(population) as 'max', min(population) as [min] FROM 'https://tinyurl.com/24buj7mb' GROUP BY state
```

## HAVING

The HAVING clause is used to specify a boolean condition that must be satisfied by a group for the group record to be output. Groups that do not satisfy the condition are discarded.

```sql
SELECT state, min(population) FROM 'https://tinyurl.com/24buj7mb' GROUP BY state HAVING max(population) > 7000000
```

## WHERE

The WHERE clause is used to specify a boolean condition that must be satisfied by an input record for that record to be output. Input records that do not satisfy the condition are discarded.

```sql
SELECT * FROM curl('https://tinyurl.com/24buj7mb') WHERE [year] = 2019
```

## ORDER BY

The ORDER BY clause specifies which SELECT clause field-expressions the query output records should be sorted by.

```sql
SELECT * FROM 'https://tinyurl.com/24buj7mb' ORDER BY [year], population DESC
```

## LIMIT AND OFFSET

The FETCH and OFFSET clauses specifies how many records should be returned.

```sql
SELECT * FROM 'https://tinyurl.com/24buj7mb' OFFSET 2 FETCH FIRST 5 ROWS
```

Also, the LIMIT clause is support for those who used to use it. But it is out of SQL standard. It is much more preferable to use FETCH clause for such cases.
