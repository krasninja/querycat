# LTSV

Labeled Tab-separated Values (LTSV) format is a variant of Tab-separated Values (TSV). Each record in a LTSV file is represented as a single line. Each field is separated by TAB and has a label and a value. The label and the value have been separated by ':'. With the LTSV format, you can parse each line by spliting with TAB (like original TSV format) easily, and extend any fields with unique labels in no particular order.

## Syntax

```
ltsv(): object<IRowsFormatter>
```

## Links

- http://ltsv.org
