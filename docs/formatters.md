# Formatters

"Formatters" are special kind of objects (of type `IRowsFormatter`) that contain the logic of data parse.

| Name and Description |
| --- |
| `csv(has_header?: boolean): object<IRowsFormatter>`<br /><br /> Comma separated values (CSV) format. |
| `iisw3c(): object<IRowsFormatter>`<br /><br /> IIS W3C log files formatter. |
| `grok(pattern: string): object<IRowsFormatter>`<br /><br /> Grok expression formatter. |
| `json(): object<IRowsFormatter>`<br /><br /> JSON formatter. |
| `regex(pattern: string, flags?: string): object<IRowsFormatter>`<br /><br /> Regular expression formatter. |
| `srt(path: string): object<IRowsFormatter>`<br /><br /> SubRip (SRT) formatter. |
| `text_line(): object<IRowsFormatter>`<br /><br /> Text line formatter. |
| `tsv(has_header?: boolean): object<IRowsFormatter>`<br /><br /> Tab separated values (TSV) format. |
| `xml(): object<IRowsFormatter>`<br /><br /> XML formatter. |
