# JSON

JSON (JavaScript Object Notation) is a lightweight data-interchange format. It is easy for humans to read and write. It is easy for machines to parse and generate. It is based on a subset of the JavaScript Programming Language Standard ECMA-262 3rd Edition - December 1999. JSON is a text format that is completely language independent but uses conventions that are familiar to programmers of the C-family of languages, including C, C++, C#, Java, JavaScript, Perl, Python, and many others. These properties make JSON an ideal data-interchange language.

## Syntax

```
json(jsonpath?: string, indent?: int): object<IRowsFormatter>
```

Parameters:

- `jsonpath`. Evaluate JSON path expression on data before processing.
- `indent`. Defines the indentation size for JSON output.

## Examples

**Select from JSON file**

```
select * from 'json_file.json';
```

**Write to JSON file**

```
select 'test' as 'propertyName' into write_file('/tmp/test.json', json());
```

```bash
$ cat /tmp/test.json 
[{"propertyName":"test"}]
```

**Query JSON path**

```
select *number* from 'person.json??$.phoneNumbers.*';
```

**Get quick info from JSON log from Google Cloud export**

```
select
  insertId as id,
  url,
  substr(msg, 0, 200) as 'message'
from (
  select "timestamp", insertId, jsonPayload, json_value(jsonPayload, '$.context.httpRequest.url') as url,
    json_value(jsonPayload, '$.message') as msg from '/home/ivan/Downloads/downloaded-logs.json' where length(jsonPayload) > 0
) where 1=1
    and jsonPayload not like '%The remote host closed the connection.%'
    and jsonPayload not like '%The client disconnected.%'
    and jsonPayload not like '%System.Threading.ThreadAbortException%';
```

**Write with indent**

```bash
qcat query --var f=~/temp/hackers.csv "select top 1 * into - format json(indent => 2) from f"
```

Result:

```json
[
  {
    "filename": "/home/ivan/temp/hackers.csv",
    "Id": 0,
    "Name": "Nigel Zboncak",
    "Balance": 582,
    "EthereumAddress": "0xf48fb3caba5c8a78ea3a620f6173391058fe58e4",
    "Phrase": "Use the haptic XSS microchip, then you can calculate the haptic microchip!",
    "CreatedAt": "02/08/2022 11:36:22",
    "RemovedAt": ""
  }
]
```
