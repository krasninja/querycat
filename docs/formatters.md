# Formatters

"Formatters" are special kind of objects (of type `IRowsFormatter`) that contains the logic data parse.

| Name | Description |
| --- | --- |
| `csv(has_header?: boolean): object<IRowsFormatter>` | Comma separated values (CSV) format. |
| `iisw3c(): object<IRowsFormatter>` | IIS W3C log files format. More info: https://www.w3.org/TR/WD-logfile. |
| `json(): object<IRowsFormatter>` | JSON formatter. |
| `text_line(): object<IRowsFormatter>` | Text line formatter. |
| `tsv(has_header?: boolean): object<IRowsFormatter>` | Tab separated values (TSV) format. |
