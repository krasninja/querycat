# Grok

Grok is a great way to parse unstructured log data into something structured and queryable.

## Syntax

```
grok(pattern: string): object<IRowsFormatter>
```

## Example

**Parse systemd journal**

```
journalctl -o short-iso | qcat "select * from stdin() format grok('%{TIMESTAMP_ISO8601:date} %{HOSTNAME:host} %{PROG:proc}\[%{POSINT:pid}\]: %{GREEDYDATA:message}')"
```

**Parse Apache logs**

```
qcat --var files='./access*.log' --var pattern='%{IPV4:clientip} - - \\[%{DATA:date}\\] "%{DATA:method} %{DATA:uri} %{DATA:protocol}" %{INT:status} %{INT:length} %{QS:ref} %{QS:agent}' "select * from files format grok(pattern)"
```

**Parse nginx logs**

Nginx configuration:

```
log_format  main  '$remote_addr - $remote_user [$time_local] "$request" '
                '$status $body_bytes_sent "$http_referer" '
                '"$http_user_agent" "$http_x_forwarded_for"';
```

Query:

```
qcat query --var pattern='%{IPORHOST:remote_addr} - %{USERNAME:remote_user} \[%{HTTPDATE:time_local}\] "%{DATA:request}" %{INT:status} %{NUMBER:bytes_sent} "%{DATA:http_referer}" "%{DATA:http_user_agent}" "%{DATA:http_x_forwarded_for}"' "select * from '/var/log/nginx/access.log' format grok(pattern)"
```
```
qcat query
"select * from '/var/log/nginx/access.log' format grok('%{IPORHOST:remote_addr} - %{USERNAME:remote_user} \[%{HTTPDATE:time_local}\] \"%{DATA:request}\" %{INT:status} %{NUMBER:bytes_sent} \"%{DATA:http_referer}\" \"%{DATA:http_user_agent}\" \"%{DATA:http_x_forwarded_for}\"')"
```

## Links

- https://www.elastic.co/guide/en/logstash/current/plugins-filters-grok.html
- https://github.com/hpcugent/logstash-patterns/blob/master/files/grok-patterns
