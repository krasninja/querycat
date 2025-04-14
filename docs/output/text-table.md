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

## Layout

There are different tables formats available:

### Table1 (Default)

```
$ qcat query --output-style Table1 --var movies=./Data/MoviesTitles.csv "select top 10 * except m.filename from movies m"

| m.imdb_id  | m.rating_id | m.title           | m.year | m.director        | m.runtime | m.imdb_rating |
| ---------- | ----------- | ----------------- | ------ | ----------------- | --------- | ------------- |
| tt0468569  | 4           | The Dark Knight   | 2008   | Christopher Nolan | 152       | 9.00          |
| tt0944947  | 105         | Game of Thrones   | 2011   | N/A               | 57        | 9.20          |
| tt9174582  | 105         | Brassic           | 2019   | N/A               | 50        | 8.40          |
| tt2442560  | 105         | Peaky Blinders    | 2013   | N/A               | 60        | 8.80          |
| tt8090284  | 1           | Tri kota          | 2015   | N/A               | 5         | 7.10          |
| tt6966692  | 4           | Green Book        | 2018   | Peter Farrelly    | 130       | 8.20          |
| tt0099785  | 3           | Home Alone        | 1990   | Chris Columbus    | 103       | 7.70          |
| tt0095016  | 5           | Die Hard          | 1988   | John McTiernan    | 132       | 8.20          |
| tt0120201  | 5           | Starship Troopers | 1997   | Paul Verhoeven    | 129       | 7.30          |
| tt0112573  | 5           | Braveheart        | 1995   | Mel Gibson        | 178       | 8.40          |
```

### Table2

```
$ qcat query --output-style Table2 --var movies=./Data/MoviesTitles.csv "select top 10 * except m.filename from movies m"

m.imdb_id  | m.rating_id | m.title           | m.year | m.director        | m.runtime | m.imdb_rating 
-----------+-------------+-------------------+--------+-------------------+-----------+---------------
tt0468569  | 4           | The Dark Knight   | 2008   | Christopher Nolan | 152       | 9.00          
tt0944947  | 105         | Game of Thrones   | 2011   | N/A               | 57        | 9.20          
tt9174582  | 105         | Brassic           | 2019   | N/A               | 50        | 8.40          
tt2442560  | 105         | Peaky Blinders    | 2013   | N/A               | 60        | 8.80          
tt8090284  | 1           | Tri kota          | 2015   | N/A               | 5         | 7.10          
tt6966692  | 4           | Green Book        | 2018   | Peter Farrelly    | 130       | 8.20          
tt0099785  | 3           | Home Alone        | 1990   | Chris Columbus    | 103       | 7.70          
tt0095016  | 5           | Die Hard          | 1988   | John McTiernan    | 132       | 8.20          
tt0120201  | 5           | Starship Troopers | 1997   | Paul Verhoeven    | 129       | 7.30          
tt0112573  | 5           | Braveheart        | 1995   | Mel Gibson        | 178       | 8.40
```

### NoSpaceTable

```
$ qcat query --output-style NoSpaceTable --var movies=./Data/MoviesTitles.csv "select top 10 * except m.filename from movies m"

|m.imdb_id|m.rating_id|m.title|m.year|m.director|m.runtime|m.imdb_rating|
|tt0468569|4|The Dark Knight|2008|Christopher Nolan|152|9.00|
|tt0944947|105|Game of Thrones|2011|N/A|57|9.20|
|tt9174582|105|Brassic|2019|N/A|50|8.40|
|tt2442560|105|Peaky Blinders|2013|N/A|60|8.80|
|tt8090284|1|Tri kota|2015|N/A|5|7.10|
|tt6966692|4|Green Book|2018|Peter Farrelly|130|8.20|
|tt0099785|3|Home Alone|1990|Chris Columbus|103|7.70|
|tt0095016|5|Die Hard|1988|John McTiernan|132|8.20|
|tt0120201|5|Starship Troopers|1997|Paul Verhoeven|129|7.30|
|tt0112573|5|Braveheart|1995|Mel Gibson|178|8.40|
```

### Card

```
$ qcat query --output-style Card --var movies=./Data/MoviesTitles.csv "select top 10 * except m.filename from movies m"

  m.imdb_id : tt0468569
m.rating_id : 4
    m.title : The Dark Knight
     m.year : 2008
 m.director : Christopher Nolan
  m.runtime : 152
m.imdb_rating : 9.00

  m.imdb_id : tt0944947
m.rating_id : 105
    m.title : Game of Thrones
     m.year : 2011
 m.director : N/A
  m.runtime : 57
m.imdb_rating : 9.20

  m.imdb_id : tt9174582
m.rating_id : 105
    m.title : Brassic
     m.year : 2019
 m.director : N/A
  m.runtime : 50
m.imdb_rating : 8.40

  m.imdb_id : tt2442560
m.rating_id : 105
    m.title : Peaky Blinders
     m.year : 2013
 m.director : N/A
  m.runtime : 60
m.imdb_rating : 8.80
```
