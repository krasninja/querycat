# Web Server

The QueryCat has internal simple web server. You can also specify what network interface and port to use. Use the command below to run it.

Features:

- Simple web UI.
- Basic authentication.
- CORS.
- JSON/HTML/Text output.
- Files browser.
- Partial requests (RANGE HTTP header).
- Show basic system info.
- IP filtering.

```
$ qcat serve
```

**Note: Do not run web server on external interface (that is accessable over internal or external network). It doesn't support any authentication methods and there is not SSL support as well. Setup any proxy (like nginx) in front of it.**

There are supported input and output types. See the example below.

## Plain Text

```bash
$ curl http://localhost:6789/api/query -d "select 2+2 as result"
```
```raw
| result |
| ------ |
| 4      |
```

## JSON

```bash
$ curl http://localhost:6789/api/query -s -H "Content-Type: application/json" -d '{"query": "select 2+2 as result"}' | jq
```
```json
{
  "schema": [
    {
      "name": "result",
      "type": "Integer",
      "description": ""
    }
  ],
  "data": [
    {
      "result": 4
    }
  ]
}
```

## HTML

```bash
$ curl http://localhost:6789/api/query -H "Accept: text/html" -d "select 2+2 as result"
```
```html
<!DOCTYPE html><HTML><BODY><TABLE>
<TR>
<TH>result</TH>
</TR>
<TR>
<TD>4</TD>
</TR>
</TABLE></BODY></HTML>
```

## Use GET Method

```bash
$ curl --get "http://localhost:6789/api/query" --data-urlencode "q=2+2"
```
