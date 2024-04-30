# Object

The functions to process POCOs (Plain Old CLR Object).

| Name and Description |
| --- |
| `object_query(obj: any, query: string): any`<br /><br /> Extracts a scalar value from a POCO .NET object. |

Examples:

```
select object_query(__data, "Address.City") from users;
```
