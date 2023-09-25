# Output Functions

The output functions return object of type `IRowsOutput`.

| Name and Description |
| --- |
| `stdout(fmt?: object<IRowsFormatter>, page_size: integer = 10): object<IRowsOutput>`<br /><br /> Writes data to the system console. |
| `write(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>` <br /><br /> Write data to an URI. |
| `write_file(path: string, fmt?: object<IRowsFormatter>): object<IRowsOutput>` <br /><br /> Writes data to a file. If `fmt` is omitted the formatter will be resolved by file extension. |
