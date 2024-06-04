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

**Parse Apache logs"

```
qcat --var files='./access*.log' --var pattern='%{IPV4:clientip} - - \\[%{DATA:date}\\] "%{DATA:method} %{DATA:uri} %{DATA:protocol}" %{INT:status} %{INT:length} %{QS:ref} %{QS:agent}' "select * from files format grok(pattern)"
```

## Links

- https://www.elastic.co/guide/en/logstash/current/plugins-filters-grok.html
- https://github.com/hpcugent/logstash-patterns/blob/master/files/grok-patterns
