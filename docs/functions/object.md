# Object

The functions to process POCOs (Plain Old CLR Object).

| Name and Description |
| --- |
| `object_query(obj: void, query: string): string`<br /><br /> Extracts a scalar value from a POCO .NET object. |

Examples:

```
select object_query(__data, "Address.City") from users;
```
