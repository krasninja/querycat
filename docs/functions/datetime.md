# Date and Time Functions

| Name and Description |
| --- |
| `date(datetime: timestamp): timestamp`<br /><br /> Takes the date part. |
| `date_part(field: string, source: timestamp): integer`<br /><br /> The function retrieves subfields such as year or hour from date/time values. |
| `now(): timestamp`<br /><br /> Current date and time |
| `to_date(target: string, fmt: string): timestamp`<br /><br /> Converts string to date according to the given format. |

The `interval` type is also supported. It can be applied to timestamp values using `+` and `-` operators. Examples:

- Add 1 day to the specific date: `select cast('2022-01-01' as timestamp) + interval '1d'` -> `01/02/2022 00:00:00`.
- Remove 1 day and 25 seconds: `select cast('2022-01-01' as timestamp) - interval '1 day 24 seconds' - interval '1s'` -> `12/30/2021 23:59:35`.

The following date and time parts are supported:

- `ms`, `milliseconds`, `milliseconds`;
- `s`, `sec`, `second`, `seconds`;
- `m`, `min`, `minute`, `minutes`;
- `h`, `hour`, `hours`;
- `d`, `day`, `days`;

Since `interval` is based on [TimeSpan](https://learn.microsoft.com/en-us/dotnet/api/system.timespan?view=net-6.0) .NET type, it represents time interval only. It cannot be used to present month, quarter, year.

## Convert To String

You can use `to_char` function for date/time text representation. Example:

```
select
    to_char(CURRENT_TIMESTAMP, 'yyyy-MM-dd ss:hh:mm z'),
    to_char(CURRENT_DATE, 'D'),
    to_char(cast('2023-01-01' as timestamp), 'dddd');

| column1                | column2                    | column3    |
| ---------------------- | -------------------------- | ---------- |
| 2022-10-19 34:10:32 +7 | Wednesday, 19 October 2022 | Sunday     |
```

The formatting is based on .NET framework conventions. You can read more about it using the links below:

- [Custom date and time format strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings).
- [Standard date and time format strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings).

## Date Part

To get the date part use `EXTRACT` function. The syntax is `EXTRACT(part FROM timestamp)`. The valid parts are:

- `YEAR`;
- `MONTH`;
- `DAY`;
- `HOUR`;
- `MINUTE`;
- `SECOND`;
- `DAY`;
- `HOUR`;
- `MINUTE`;
- `SECOND`;
- `MILLISECOND`;

Example:

```
select extract(year from cast('2023-01-01' as timestamp));
```
