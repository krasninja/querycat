# Input Functions

The input functions returns object of type `IRowsInput`.

| Name and Description |
| --- |
| `buffer_input(input: object<IRowsInput>, size: integer := 1024): object<IRowsInput>`<br /><br /> Implements buffer for rows input. |
| `curl(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Reads data from a remote server using HTTP transport protocol. |
| `delay_input(input: object<IRowsInput>, delay_secs: integer := 5): object<IRowsInput>`<br /><br /> Implements delay before reading the next record. |
| `generate_series(start: integer, stop: integer, step: integer = 1): object<IRowsInput>`<br />`generate_series(start: float, stop: float, step: float = 1): object<IRowsInput>`<br />`generate_series(start: numeric, stop: numeric, step: numeric = 1): object<IRowsInput>`<br />`generate_series(start: timestamp, stop: timestamp, step: timestamp): object<IRowsInput>`<br /><br />Generates a series of values from start to stop, with a step size of step. |
| `ls_dir(path: string): object<IRowsInput>`<br /><br /> List directory content (files and sub-directories). |
| `read(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Read data from a URI. |
| `read(blob: blob, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Read data from a BLOB. |
| `read_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Reads data from a file. If `fmt` is ommited the formatter will be resolved by file extension. |
| `read_text(text: string, fmt: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Reads data from a string. The `format` should be presented. |
| `retry_input(input: object<IRowsInput>, max_attempts: integer := 3, retry_interval_secs: float := 5.0): object<IRowsInput>`<br /><br /> Implements retry resilience strategy with constant delay interval for rows input. |
| `stdin(skip_lines: integer = 0, fmt?: object<IRowsFormatter>): object<IRowsInput>`<br /><br /> Read data from the system standard input. |
