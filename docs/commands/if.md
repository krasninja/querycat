# IF

IF statements let you execute commands based on certain conditions.

## Syntax

```
IF condition THEN block
[ ELSEIF condition THEN block [, ...] ]
[ ELSE block ]
```

## Examples

```
IF 1=2 THEN
BEGIN
    SELECT 1;
END
ELSEIF 4=6 OR 2=2 THEN
BEGIN
    SELECT 2;
END
ELSEIF 3=6 THEN
BEGIN
    SELECT 3;
END
ELSE
BEGIN
    SELECT 4;
END
```
