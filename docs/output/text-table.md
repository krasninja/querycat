# Text Table

The text table output renders the rows in a plain text format. The "|" is used to separated fields. Example:

```
$ qcat "SELECT * FROM '/home/ivan/1/data.tsv'"

| /home/ivan/1/data.tsv | date                | name       | action                        |
| --------------------- | ------------------- | ---------- | ----------------------------- |
| /home/ivan/1/data.tsv | 10/02/2022 10:23:53 | Ivan       | Log-in                        |
| /home/ivan/1/data.tsv | 10/02/2022 10:27:14 | Ivan       | Start application "quake.exe" |
| /home/ivan/1/data.tsv | 10/02/2022 10:35:50 | Ivan       | Log-out                       |
```

This is the default output format if nothing has been is specified.
