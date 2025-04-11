# Misc Functions

| Name and Description |
| --- |
| `cast(expression as type): any`<br /><br /> The function convert an expression of one data type to another. |
| `cache_input(input: object<IRowsIterator>, key: string, expire?: interval := null): object<IRowsIterator>`<br />`cache_input(input: object<IRowsInput>, key: string, expire?: interval := null): object<IRowsIterator>`<br /><br />Implements rows input caching. |
| `coalesce(...args: any[]): any`<br /><br /> The COALESCE function accepts an unlimited number of arguments. It returns the first argument that is not null. If all arguments are null, the COALESCE function will return null. |
| `uuid(): string`<br /><br /> The function returns a version 4 (random) UUID. |
| `nop(...args: any[]): void`<br /><br />Not operation. The function can be used to suppress output. |
| `nullif(value1: any, value2: any): any`<br /><br /> The function returns a null value if value1 equals value2; otherwise it returns value1. |
| `self(target: any): any`<br /><br /> Returns the object itself. Needed when you need to pass variable as function call. |
| `size_pretty(size: integer, base: integer = 1024): string`<br /><br /> Converts a size in bytes into a more easily human-readable format with size units. |

## Type Cast

There are many cases that you want to convert a value of one data type into another. QueryCat provides you with the CAST operator that allows you to do this. Also, PostgreSQL style cast operator is supported. These two statements below are equal:

```
CAST('10' as integer)
'10'::integer
```
