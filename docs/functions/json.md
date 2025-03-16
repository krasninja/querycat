# JSON

The functions to process JSON strings.

| Name and Description |
| --- |
| `is_json(json: string): boolean`<br /><br /> Tests whether a string contains valid JSON. |
| `json_array_elements(json: string): object<IRowsIterator>`<br /><br /> Expands the top-level JSON array into a set of values. |
| `json_array_length(json: string): integer`<br /><br /> Returns the number of elements in the top-level JSON array. |
| `json_query(json: string, query: string): string`<br /><br /> Extracts an object or an array from a JSON string. |
| `json_value(json: string, query: string): any`<br /><br /> Extracts a scalar value from a JSON string. |
| `to_json(obj: object): string`<br /><br /> Constructs JSON text from object. |

## Examples

**Query JSON files**

```sql
select
  id,
  json_query(Content, '$.request') as r
from '*.json'
where
  json_value(Data, '$.method') = 'POST'
  and json_value(Content, '$.request.model.RegionID')::int = 14321;"
```
