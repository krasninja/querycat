# Misc Functions

| Name and Description |
| --- |
| `cast(expression as type): any`<br /><br /> The function convert an expression of one data type to another. |
| `coalesce(...args: any[]): any`<br /><br /> The COALESCE function accepts an unlimited number of arguments. It returns the first argument that is not null. If all arguments are null, the COALESCE function will return null. |
| `uuid(): string`<br /><br /> The function returns a version 4 (random) UUID. |
| `nullif(value1: any, value2: any): any`<br /><br /> The function returns a null value if value1 equals value2; otherwise it returns value1. |

## Type Cast

There are many cases that you want to convert a value of one data type into another. QueryCat provides you with the CAST operator that allows you to do this. Also, PostgreSQL style cast operator is supported. These two statements below are equal:

```
CAST('10' as integer)
'10'::integer
```
