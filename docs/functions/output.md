# Output Functions

The output functions return object of type `IRowsOutput`.

| Name and Description |
| --- |
| `buffer_output(output: object<IRowsOutput>, size: integer := 1024): object<IRowsOutput>`<br /><br /> Implements buffer for rows output. |
| `delay_output(output: object<IRowsOutput>, delay_secs: integer := 5): object<IRowsOutput>`<br /><br /> Implements delay before writing values. |
| `stdout(fmt?: object<IRowsFormatter>): object<IRowsOutput>`<br /><br /> Writes data to the system console. |
| `parallel_output(output: object<IRowsOutput>, max_degree?: integer): object<IRowsOutput>`<br /><br /> Allows to run output write operations in parallel. Must be used only for rows outputs that support this! |
| `retry_output(output: object<IRowsOutput>, max_attempts: integer := 3, retry_interval_secs: float := 5.0): object<IRowsOutput>`<br /><br /> Implements retry resilience strategy with constant delay interval for rows output. |
| `write(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>` <br /><br /> Write data to an URI. |
| `write_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsOutput>` <br /><br /> Writes data to a file. If `fmt` is omitted the formatter will be resolved by file extension. |
