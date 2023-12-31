# Grok

Grok is a great way to parse unstructured log data into something structured and queryable.

## Syntax

```
grok(pattern: string): object<IRowsFormatter>
```

## Example

** Parse systemd journal**

```
journalctl -o short-iso | qcat "select * from stdin() format grok('%{TIMESTAMP_ISO8601:date} %{HOSTNAME:host} %{PROG:proc}\[%{POSINT:pid}\]: %{GREEDYDATA:message}')"
```

## Links

- https://www.elastic.co/guide/en/logstash/current/plugins-filters-grok.html
- https://github.com/hpcugent/logstash-patterns/blob/master/files/grok-patterns
