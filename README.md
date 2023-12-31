# QueryCat

QueryCat is the command line tool that provides the universal access to a text-based data like CSV, JSON, XML and log files. It also provides the way to query this data using SQL (Structured Query Language). The general idea is to bring well-known SQL expressive power and provide easy access to various kind of information.

- QueryCat is designed to use pipeline mode to be able to process large amount of data.
- INTO clause allows to write transformed data to popular formats.
- The single binary file doesn't require any dependencies to be installed and simplifies installation (download and use).

## Features

1. Can query CSV, TSV, JSON, XML, log files and other input sources.
2. Has simple SQL-engine that supports select, filter, [group](https://querycat.readthedocs.io/en/latest/functions/aggregate/), joins, order, limit, offset, union, subqueries, CTE (recursive CTE), window functions.
3. JSONPath, XPath expressions are supported.
4. Supports regex and Grok patterns.
5. Primitive [Web UI](https://querycat.readthedocs.io/en/latest/features/web-server/) and [REST API](https://querycat.readthedocs.io/en/latest/features/web-server/) are available.
6. Can be extended using [plugins](https://querycat.readthedocs.io/en/latest/plugins/).
7. [Variables](https://querycat.readthedocs.io/en/latest/commands/declare/) support.

## In Action

Count total number of processes by user.

```
$ ps -ef | qcat "SELECT UID, COUNT(*) cnt FROM - GROUP BY UID ORDER BY cnt DESC LIMIT 3"
| UID        | cnt   |
| ---------- | ----- |
| root       | 256   |
| ivan       | 93    |
| systemd+   | 1     |
```

Calculate the total files size per user in a directory.

```
$ find /tmp -ls 2>/dev/null | qcat "SELECT column4 as user, size_pretty(SUM(column6)) size FROM - GROUP BY column4"
| user       | size       |
| ---------- | ---------- |
| root       | 1.5K       |
| ivan       | 21.3M      |
```

Select JSON logs exported from Google Cloud.

```
$ qcat 'SELECT [timestamp], insertId, json_value(jsonPayload, "$.context.httpRequest.url") as url, SUBSTR(json_value(jsonPayload, "$.message"), 0, 50) AS msg FROM "downloaded-logs.json" WHERE length(jsonPayload) > 0 ORDER BY [timestamp]'
| timestamp           | insertId          | url        | msg                                                |
| ------------------- | ----------------- | ---------- | -------------------------------------------------- |
| 03/30/2023 13:33:22 | gh53av89ifehmpo5e | NULL       | "An TLS 1.1 connection request was received from a |
| 03/30/2023 13:33:22 | gh53av89ifehmpo5d | NULL       | "An TLS 1.0 connection request was received from a |
| 03/30/2023 13:33:23 | gh53av89ifehmpo5o | NULL       | "An TLS 1.1 connection request was received from a |
| 03/30/2023 13:33:24 | gh53av89ifehmpo5r | NULL       | "An TLS 1.2 connection request was received from a |
| 03/30/2023 13:44:08 | gh53av89ifehmpo6b | NULL       | "An error occurred while using SSL configuration f |
| 03/30/2023 13:51:38 | gh53av89ifehmpo6d | NULL       | "An error occurred while using SSL configuration f |
| 03/30/2023 13:53:38 | rh4iofg12znj07    | "https://XXX/client/abort?transport=serverSentEvents\u0026clientProtocol=2.1\" | "System.Web.HttpException (0x80070057): The remote |
| 03/30/2023 13:53:51 | svb32tg14gii2c    | "https://XXX/client/abort?transport=serverSentEvents\" | "System.Web.HttpException (0x80070057): The remote |
```

Show full user name for `ps` command by joining with `/etc/passwd` file.

```
$ ps aux | qcat "SELECT column4 USER, ps.PID, ps.COMMAND FROM - AS ps JOIN (SELECT * FROM '/etc/passwd' FORMAT csv(delimiter=>':', has_header=>false)) psw ON ps.USER = psw.column0"
| USER       | ps.PID | ps.COMMAND                    |
| ---------- | ------ | ----------------------------- |
|            | 1      | /sbin/init                    |
|            | 2      | [kthreadd]                    |
| PolicyKit daemon | 976 | /usr/lib/polkit-1/polkitd  |
```

Query CSV file with "a" and "b" numeric columns.

```
$ qcat --var csv=/tmp/1.csv "select a + b from csv"
3
7
```

## Information

- Home: [GitHub](https://github.com/krasninja/querycat)
- Tutorial: [ReadTheDocs](https://querycat.readthedocs.io/en/latest/tutorial/)
- Documentation: [ReadTheDocs](https://querycat.readthedocs.io/)

## Limitations

- Not the whole SQL standard is implemented. It is huge.
- Only limited amount of rows sources support INSERT and UPDATE command.
