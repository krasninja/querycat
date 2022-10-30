# String Functions

| Name and Description |
| --- |
| `btrim(target: string, characters: string = ' '): string`<br /><br /> Remove the longest string consisting only of characters in characters from the start and end of string. |
| `length(target: string): integer`<br />`char_length(target: string): integer`<br />`character_length(target: string): integer`<br /><br /> Number of characters in string. |
| `lower(target: string): string`<br /><br /> Convert a string to lower case. |
| `ltrim(target: string, characters: string = ' '): string`<br /><br /> Removes the longest string containing only characters in characters from the start of string. |
| `[position](substring: string, target: string): integer`<br /><br /> Returns first starting index of the specified substring within string, or zero if it's not present. |
| `replace(target: string, old: string, new: string): string`<br /><br /> Replaces all occurrences in string of substring from with substring to. |
| `reverse(target: string): string`<br /><br /> Reverses the order of the characters in the string. |
| `rtrim(target: string, characters: string = ' '): string`<br /><br /> Removes the longest string containing only characters in characters from the end of string. |
| `substr(target: string, start: integer, count?: integer): string`<br /><br /> Extracts the substring of string starting at the start'th character, and extending for count characters if that is specified. |
| `to_char(args: any, fmt?: string): string`<br /><br /> Convert value to string according to the given format. |
| `upper(target: string): string`<br /><br /> Convert a string to upper case. |
