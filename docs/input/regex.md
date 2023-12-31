# Regular Expressions

Regular expression is a sequence of characters that specifies a match pattern in text.

## Syntax

```
regex(pattern: string, flags?: string): object<IRowsFormatter>
```

## Example

**Parse Nginx access.log file**

```
select * from '/var/log/access.log' format
  regex('(?<ip>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})?(?<ip2>, \d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})? -? -?\S* \[(?<timestamp>\d{2}\/\w{3}\/\d{4}:\d{2}:\d{2}:\d{2} (\+|\-)\d{4})\] "(?<method>\S{3,10}) (?<path>\S+) HTTP\/1\.\d" (?<response_status>\d{3}) (?<bytes>\d+) "(?<referer>(\-)|(.+))?" "(?<useragent>.+)"');
```
