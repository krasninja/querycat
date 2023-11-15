# Formatters

"Formatters" are special kind of objects (of type `IRowsFormatter`) that contain the logic of data parse.

| Name | Description |
| --- | --- |
| `csv(has_header?: boolean): object<IRowsFormatter>` | Comma separated values (CSV) format. |
| `json(): object<IRowsFormatter>` | JSON formatter. |
| `regex(pattern: string): object<IRowsFormatter>` | Regular expression formatter. |
| `text_line(): object<IRowsFormatter>` | Text line formatter. |
| `tsv(has_header?: boolean): object<IRowsFormatter>` | Tab separated values (TSV) format. |
| `xml(): object<IRowsFormatter>` | XML formatter. |
