# Formatters

"Formatters" are special kind of objects (of type `IRowsFormatter`) that contains the logic data parse.

| Name | Description |
| --- | --- |
| `csv(has_header?: boolean): object<IRowsFormatter>` | Comma separated values (CSV) format. |
| `tsv(has_header?: boolean): object<IRowsFormatter>` | Tab separated values (TSV) format. |
| `iisw3c(): object<IRowsFormatter>` | IIS W3C log files format. More info: https://www.w3.org/TR/WD-logfile. |
