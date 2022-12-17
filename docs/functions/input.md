# Input Functions

The input functions returns object of type `IRowsInput`.

| Name and Description |
| --- |
| `curl(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Reads data from a remote server using HTTP transport protocol. |
| `read(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Read data from a URI. |
| `read_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Reads data from a file. If `fmt` is ommited the formatter will be resolved by file extension. |
| `read_text(text: string, fmt: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Reads data from a string. The `format` should be presented. |
| `stdin(skip_lines: integer = 0, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Read data from the system standard input. |
