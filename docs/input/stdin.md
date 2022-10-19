# Standard Input

Standard input is a stream from which a program reads its input data. The following example shows the basic usage in Linux.

```
# Redirect ls command output to qcat utility. skip_lines parameter allows skip first specific
# amount of lines because sometimes there might be metadata.
$ ls /usr/bin -la | qcat "select * from stdin(skip_lines=>1) offset 2 fetch 10;"
| column0    | column1    | column2    | column3    | column4    | column5    | column6    | column7    | column8           |
| ---------- | ---------- | ---------- | ---------- | ---------- | ---------- | ---------- | ---------- | ----------------- |
| lrwxrwxrwx | 1          | root       | root       | 9          | окт        | 12         | 03:04      | 2to3              |
| -rwxr-xr-x | 1          | root       | root       | 96         | окт        | 12         | 03:04      | 2to3-3.10         |
| -rwxr-xr-x | 1          | root       | root       | 14168      | авг        | 26         | 01:51      | 411toppm          |
| -rwxr-xr-x | 1          | root       | root       | 14256      | мая        | 12         | 04:24      | 4channels         |
| -rwxr-xr-x | 1          | root       | root       | 36         | сен        | 8          | 2021       | 7z                |
| -rwxr-xr-x | 1          | root       | root       | 37         | сен        | 8          | 2021       | 7za               |
| -rwxr-xr-x | 1          | root       | root       | 37         | сен        | 8          | 2021       | 7zr               |
| -rwxr-xr-x | 1          | root       | root       | 18456      | апр        | 14         | 2022       | CreateDOMDocument |
| -rwxr-xr-x | 1          | root       | root       | 30736      | апр        | 14         | 2022       | DOMCount          |
| -rwxr-xr-x | 1          | root       | root       | 30768      | апр        | 14         | 2022       | DOMPrint          |

# Redirect ls command output to a file.
$ ls -la /usr/bin > /tmp/out.txt

# Redirect from file to qcat.
$ qcat "select * from stdin(skip_lines=>1) offset 2 fetch 10;" < /tmp/1.txt
```

Example in Windows command line:

```
> D:\apps>dir | qcat "select * from stdin(skip_lines=>5)"
| column0    | column1    | column2    | column3     | column4             |
| ---------- | ---------- | ---------- | ----------- | ------------------- |
| 10/19/2022 | 10:23      | PM         | <DIR>       | .                   |
| 10/19/2022 | 10:23      | PM         | <DIR>       | ..                  |
| 10/19/2022 | 10:23      | PM         | 0           | 1.txt               |
| 10/03/2022 | 12:44      | PM         | <DIR>       | ffmpeg              |
| 09/06/2022 | 11:14      | AM         | <DIR>       | LPSV2.D1            |
| 09/07/2022 | 10:11      | PM         | <DIR>       | openssl-3           |
| 07/04/2022 | 10:12      | AM         | <DIR>       | procexp             |
| 07/26/2022 | 05:55      | PM         | <DIR>       | procmon             |
| 10/19/2022 | 10:21      | PM         | 35,160,585  | qcat.exe            |
| 04/01/2022 | 08:34      | PM         | 157,320,069 | saritasa-notify.exe |
| 09/12/2022 | 12:02      | PM         | <DIR>       | saxon               |
| 09/27/2022 | 12:16      | AM         | 40          | test.csv            |
| 4          | File(s)    | 192,480,694 | bytes       |  bytes
            |
| 8          | Dir(s)     | 89,277,165,568 | bytes       | free                |
```
