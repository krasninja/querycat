# CSV

The CSV input format parses comma-separated values text files. In a CSV text file, each line consists of one record, and fields in a record are separated by commas. Depending on the application, the first line in a CSV file might be a "header", containing the labels of the record fields. The QueryCat can analyze the first rows to understand whether a file has header or not. Here is the sample CSV file with header:

```raw
date,name,action
10/02/2022 10:23:53,Ivan,Log-in
10/02/2022 10:27:14,Ivan,"Start application ""quake.exe"""
10/02/2022 10:35:50,Ivan,Log-out
```

Field values and labels might be enclosed within double-quote (") characters.

## Syntax

```
csv(has_header?: boolean): object<IRowsFormatter>
```

Parameters:

- `has_header`. Should be `true` if an input has header, `false` otherwise.

## Examples

**CSV rows count**

```sql
SELECT COUNT(*) FROM 'example.csv'
```

**Spread records to different files based on column value**

```sql
SELECT * INTO write_file('/tmp/data-' || city || '.csv', csv()) FROM './Simple2.csv'
```
