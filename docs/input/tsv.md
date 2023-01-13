# TSV

The [TSV](https://en.wikipedia.org/wiki/Tab-separated_values) input format parses tab-separated and space-separated values text files.

TSV text files, usually called "tabular" files, are generic text files containing values separated by either spaces or tabs. This is also the format of the output of many command-line tools. Here is the example of TSV file:

```
date	name	action
10/02/2022 10:23:53 Ivan	Log-in
10/02/2022 10:27:14 Ivan	"Start application ""quake.exe"""
10/02/2022 10:35:50 Ivan	Log-out
```

## Syntax

```
tsv(has_header?: boolean, quote_strings?: boolean = false): object<IRowsFormatter>
```

Parameters:

- `has_header`. Should be `true` if an input has header, `false` otherwise.
- `quote_strings`. Force quote all strings.
