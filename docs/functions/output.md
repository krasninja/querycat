# Output Functions

The output functions return object of type `IRowsOutput`.

| Name and Description |
| --- |
| `console(formatter?: object<IRowsFormatter>, page_size: integer = 10): object<IRowsOutput>`<br /><br /> Writes data to the system console. |
| `write(uri: string, formatter?: object<IRowsFormatter>): object<IRowsInput>` <br /><br /> Write data to an URI. |
| `write_file(path: string, formatter?: object<IRowsFormatter>): object<IRowsOutput>` <br /><br /> Writes data to a file. If `formatter` is omitted the formatter will be resolved by file extension. |
