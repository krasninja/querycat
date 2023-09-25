# Input Functions

The input functions returns object of type `IRowsInput`.

| Name and Description |
| --- |
| `curl(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Reads data from a remote server using HTTP transport protocol. |
| `generate_series(start: integer, stop: integer, step: integer = 1): object<IRowsInput>`<br />`generate_series(start: float, stop: float, step: float = 1): object<IRowsInput>`<br />`generate_series(start: numeric, stop: numeric, step: numeric = 1): object<IRowsInput>`<br />`generate_series(start: timestamp, stop: timestamp, step: timestamp): object<IRowsInput>`<br /><br />Generates a series of values from start to stop, with a step size of step. |
| `read(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Read data from a URI. |
| `read_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Reads data from a file. If `fmt` is ommited the formatter will be resolved by file extension. |
| `read_text(text: string, fmt: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Reads data from a string. The `format` should be presented. |
| `stdin(skip_lines: integer = 0, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Read data from the system standard input. |
