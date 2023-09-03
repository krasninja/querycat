# SubRip

SRT (SubRip file format) is a simple subtitle file saved in the SubRip file format with the .srt extension. It contains a sequential number of subtitles, start and end timestamps, and subtitle text. SRT files make it possible to add subtitles to video content after it is produced.

## Syntax

```
srt(path: string): object<IRowsFormatter>
```

## Examples

**Parse SRT file**

```
qcat --var srt=/home/ivan/Downloads/Green\ Book.srt "select * from srt";
```

```
| counter | start_time       | end_time         | text                                                                    |
| ------- | ---------------- | ---------------- | ----------------------------------------------------------------------- |
| 1       | 00:01:03.1880000 | 00:01:05.7260000 | - Yo, Tommy!
- Hey, taxi!                                               |
| 2       | 00:01:12.7390000 | 00:01:15.4020000 | Cigars. Cigarettes.                                                     |
| 3       | 00:01:18.2450000 | 00:01:19.4060000 | Great idea.                                                             |
```
