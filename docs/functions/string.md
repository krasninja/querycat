# String Functions

| Name and Description |
| --- |
| `btrim(target: string, characters: string = ' '): string`<br /><br /> Remove the longest string consisting only of characters in characters from the start and end of string. |
| `length(target: string): integer`<br />`char_length(target: string): integer`<br />`character_length(target: string): integer`<br /><br /> Number of characters in string. |
| `lower(target: string): string`<br /><br /> Convert a string to lower case. |
| `ltrim(target: string, characters: string = ' '): string`<br /><br /> Removes the longest string containing only characters in characters from the start of string. |
| `position(substring: string, target: string): integer`<br /><br /> Returns first starting index of the specified substring within string, or zero if it's not present. |
| `regexp_count(target: string, pattern: string, start?: integer = 1): string`<br /><br /> Returns the number of times the regular expression pattern matches in the string. |
| `regexp_replace(target: string, pattern: string, replacement: string, start?: integer = 1): string`<br /><br /> Provides substitution of new text for substrings that match regular expression patterns. |
| `regexp_substr(target: string, pattern: string, start?: integer = 1, n?: integer = 1, subexpr?: integer = 1): string`<br /><br /> Returns the substring within string that matches the N'th occurrence of the regular expression pattern, or NULL. |
| `replace(target: string, old: string, new: string): string`<br /><br /> Replaces all occurrences in string of substring from with substring to. |
| `reverse(target: string): string`<br /><br /> Reverses the order of the characters in the string. |
| `rtrim(target: string, characters: string = ' '): string`<br /><br /> Removes the longest string containing only characters in characters from the end of string. |
| `split_part(target: string, delimiter: string, n: integer): string`<br /><br /> Splits string at occurrences of delimiter and returns the n'th field (counting from one). |
| `starts_with(target: string, prefix: string): boolean`<br /><br /> Returns true if string starts with prefix. |
| `string_to_table(target: string, delimiter?: string, null_string?: string := null): object<IRowsIterator>`<br /><br /> Splits the string at occurrences of delimiter and returns the resulting fields as a set of text rows. |
| `substr(target: string, start: integer, count?: integer): string`<br /><br /> Extracts the substring of string starting at the start'th character, and extending for count characters if that is specified. |
| `to_char(args: any, fmt?: string): string`<br /><br /> Convert value to string according to the given format. |
| `upper(target: string): string`<br /><br /> Convert a string to upper case. |

## SQL Standard

There are SQL standard regular expression functions that are partially supported:

| SQL Standard | Alternative |
| --- | --- |
| `OCCURRENCES_REGEX(pattern IN string)` | `regexp_count(string, pattern)` |
| `SUBSTRING_REGEX(pattern IN string)` | `regexp_substr(string, pattern)` |
| `TRANSLATE_REGEX(pattern IN string WITH replacement)` | `regexp_replace(string, pattern, replacement)` |
