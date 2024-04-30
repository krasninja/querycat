# IISW3C

The IISW3C input format parses IIS log files in the W3C Extended Log File Format. IIS web sites logging in the W3C Extended format can be configured to log only a specific subset of the available fields. Log files in this format begin with some informative headers ("directives"), the most important of which is the "#Fields" directive, describing which fields are logged at which position in a log row. After the directives, the log entries follow. Each log entry is a space-separated list of field values. If the logging configuration of an IIS virtual site is updated, the structure of the fields in the file that is currently logged to might change according to the new configuration. In this case, a new "#Fields" directive is logged describing the new fields structure, and the IISW3C input format keeps track of the structure change and parses the new log entries accordingly.

The following example shows a portion of a W3C Extended Log File Format log file:

```
#Software: Microsoft Internet Information Services 10.0
#Version: 1.0
#Date: 2022-01-22 00:00:01
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) cs(Referer) sc-status sc-substatus sc-win32-status time-taken
2022-01-22 00:00:01 10.142.0.2 POST /member/TrafficSchoolLessonTrack.ashx pid=2173020&lid=125452789&ls=0&cs=45452&st=637783764992503700 443 diegorebana3333@example.com 172.69.69.207 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/97.0.4692.99+Safari/537.36 https://member.getdefensive.com/member/TrafficSchoolLesson.aspx?id=125452789 200 0 0 31
2022-01-22 00:00:01 10.142.0.2 POST /member/TrafficSchoolLessonTrack.ashx pid=2173304&lid=125470842&ls=0&cs=27538&st=637781801140015900 443 - 172.69.69.207 Mozilla/5.0+(Macintosh;+Intel+Mac+OS+X+10_15_7)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/97.0.4692.71+Safari/537.36 https://member.getdefensive.com/member/TrafficSchoolLesson.aspx?id=125470842 200 0 0 31
```

## Syntax

```
iisw3c(): object<IRowsFormatter>
```

## Examples

**Top 20 URLs exluding websocket connections**

```sql
SELECT "cs-uri-stem", COUNT(*) AS hits
FROM 'u_ex*.log' FORMAT iisw3c()
WHERE "cs-uri-stem" NOT LIKE '%/client/%'
GROUP BY "cs-uri-stem"
ORDER BY Hits DESC FETCH 20
```

**Top 5 slow pages**

```sql
SELECT DISTINCT "cs-method", "cs-uri-stem"
FROM '*.log' FORMAT iisw3c() ORDER BY "time-taken" DESC FETCH 5
```
