# Input Functions

The input functions returns object of type `IRowsInput`.

| Name and Description |
| --- |
| `read(uri: string, formatter?: object<IRowsFormatter>): object<IRowsInput>` <br /><br /> Read data from a URI. |
| `read_file(path: string, formatter?: object<IRowsFormatter>): object<IRowsInput>` <br /><br /> Reads data from a file. If `format` is ommited the formatter will be resolved by file extension. |
| `read_string(text: string, formatter: object<IRowsFormatter>): object<IRowsInput>` <br /><br /> Reads daat from a string. The `format` should be presented. |
| `curl(uri: string, formatter?: object<IRowsFormatter>): object<IRowsInput>` <br /><br /> Reads data from a remote server using HTTP transport protocol. |
