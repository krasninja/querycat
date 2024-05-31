# SELECT

Executes the SQL query against input. If not output is specified (with `INTO` clause) the system console will be used.

## Syntax

```
[ WITH [ RECURSIVE ] with_query [, ...] ]
SELECT [ ALL | DISTINCT [ ON ( expression [, ...] ) ] ] [ TOP number ]
    [ * | expression AS [ alias ] | column AS [ alias ] [, ...] ]
[ EXCEPT column [, ...] ]
[ INTO [ function : IRowsOutput ] ]
[ FROM [ function : IRowsInput | uri ] FORMAT [ function : IRowsFormatter ] ]
[ WHERE search_condition [, ...] ]
[ GROUP BY [ expression ] [, ...] ]
[ HAVING aggregate_search_condition [, ...] ]
[ WINDOW window_name AS ( window_definition ) [, ...] ]
[ { UNION | INTERSECT | EXCEPT } [ ALL | DISTINCT ] select ]
[ ORDER BY [ expression [ ASC | DESC ] ] [ NULLS { FIRST | LAST } ] ]
[ LIMIT number ]
[ OFFSET number [ ROW | ROWS ] ]
[ FETCH [ FIRST | NEXT ] number [ ROW | ROWS ] [ ONLY ] ]
```

## WITH

The WITH clause allows you to specify one or more subqueries that can be referenced by name in the primary query. The recursive syntax is also supported.

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
SELECT [cs(User-Agent)] FROM read_file('u_ex220826.log', fmt=>iisw3c());
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

## WINDOW

A window function performs a calculation across a set of table rows that are somehow related to the current row. Right now, only PARTITION BY and ORDER clauses are supported.

## UNION

Using the operators UNION, INTERSECT, and EXCEPT, the output of more than one SELECT statement can be combined to form a single result set.
- UNION - returns all rows that are in one or both of the result sets.
- INTERSECT - returns all rows that are strictly in both result sets.
- EXCEPT -  returns the rows that are in the first result set but not in the second.
In all three cases, duplicate rows are eliminated unless ALL is specified. The noise word DISTINCT can be added to explicitly specify eliminating duplicate rows. Notice that DISTINCT is the default behavior here.

## WHERE

The WHERE clause is used to specify a boolean condition that must be satisfied by an input record for that record to be output. Input records that do not satisfy the condition are discarded.

```sql
SELECT * FROM curl('https://tinyurl.com/24buj7mb') WHERE [year] = 2019
```

*Note: The `LIKE` and `SIMILAR` statements are also supported. However, `SIMILAR` is not SQL-compliant. It doesn't support "SQL regular expression". Instead, it supports .NET compliant regular expressions.*

## ORDER BY

The ORDER BY clause specifies which SELECT clause field-expressions the query output records should be sorted by. If NULLS LAST is specified, null values sort after all non-null values; if NULLS FIRST is specified, null values sort before all non-null values.

```sql
SELECT * FROM 'https://tinyurl.com/24buj7mb' ORDER BY [year], population DESC
```

## LIMIT AND OFFSET

The FETCH and OFFSET clauses specifies how many records should be returned.

```sql
SELECT * FROM 'https://tinyurl.com/24buj7mb' OFFSET 2 FETCH FIRST 5 ROWS
```

Also, the LIMIT clause is support for those who used to use it. But it is out of SQL standard. It is much more preferable to use FETCH clause for such cases.
