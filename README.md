# QueryCat

![GitHub License](https://img.shields.io/github/license/krasninja/querycat)
![NuGet Version](https://img.shields.io/nuget/v/querycat)
![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/krasninja/querycat/total)

QueryCat is the command line tool that allows you to use SQL (Structured Query Language) to query data in CSV, JSON, XML and various log files. The general idea is to bring well-known SQL expressive power and provide easy access to various kinds of information. You won't need to upload your text data into Excel or any database table - just query it as is with QueryCat!

- It is designed to use pipeline mode to be able to process large amount of data.
- INTO clause allows to write transformed data to popular formats.
- The single binary file doesn't require any dependencies to be installed and simplifies installation (just download and use).
- You can use it as library in your .NET application.

## Features

1. Query CSV, TSV, JSON, XML, log files and other input sources.
2. Has simple SQL-engine that supports select, filter, [group](https://querycat.readthedocs.io/en/latest/functions/aggregate/), joins, order, limit, offset, union, subqueries, CTE (recursive CTE), window functions.
3. JSONPath, XPath expressions.
4. Supports regex and Grok patterns.
5. Simple [Web UI](https://querycat.readthedocs.io/en/latest/features/web-server/) and [REST API](https://querycat.readthedocs.io/en/latest/features/web-server/).
6. Simple files server (with partial request support).
7. [Plugins](https://querycat.readthedocs.io/en/latest/plugins/) ecosystem.
8. [Variables](https://querycat.readthedocs.io/en/latest/commands/declare/) support.
9. Query .NET objects in your project (if used as library).

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
$ qcat 'SELECT "timestamp", insertId, json_value(jsonPayload, \'$.context.httpRequest.url\') as url, SUBSTR(json_value(jsonPayload, \'$.message\'), 0, 50) AS msg FROM \'downloaded-logs.json\' WHERE length(jsonPayload) > 0 ORDER BY "timestamp"'
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

## NuGet Package

You can use the QueryCat library in your project. See [SDK](https://querycat.readthedocs.io/en/latest/development/sdk/) sections in the docs.

```
$ dotnet add package QueryCat
```

## Information

- Home: [GitHub](https://github.com/krasninja/querycat)
- Tutorial: [ReadTheDocs](https://querycat.readthedocs.io/en/latest/tutorial/)
- Documentation: [ReadTheDocs](https://querycat.readthedocs.io/)

## Limitations

- Not the whole SQL standard is implemented.
- Only limited amount of rows sources supports INSERT and UPDATE commands.

## License

QueryCat is licensed under the MIT License - see the [LICENSE](LICENSE.txt) file for details
