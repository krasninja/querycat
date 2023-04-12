# INSERT

Inserts data into the output source.

## Syntax

```
INSERT INTO ( function : IRowsOutput ) [ ( column_name [, ...] ) ]
[ VALUES ( value, ... ) [, ...] ]
[ select_clause ]
```

## Examples

```sql
INSERT INTO 'test.json' (a, b)
VALUES (1, 2), (3, 4);

INSERT INTO 'test.csv'
SELECT * FROM 'Countries1.csv';
```
