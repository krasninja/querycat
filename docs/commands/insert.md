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
-- Insert into JSON file values.
INSERT INTO 'test.json' (a, b)
VALUES (1, 2), (3, 4);

-- Insert without specifying columns.
INSERT INTO 'test.csv'
SELECT * FROM 'Countries1.csv';

-- Insert into variable.
INSERT INTO self(array_variable) (id, "name") VALUES (4, 'Abbie Cornish');
```
