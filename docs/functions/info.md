# Information Functions

The various information functions about data source and application. The information functions have `_` prefix.

| Name and Description |
| --- |
| `_functions(): object<IRowsIterator>`<br /><br /> Return all registered functions. |
| `_plugins(): object<IRowsIterator>`<br /><br /> Return available plugins from repository. |
| `_schema(input: object<IRowsInput>): object<IRowsIterator>`<br /><br /> Return row input columns information. |
| `_size_pretty(size: integer, base: integer = 1024): string`<br /><br /> Converts a size in bytes into a more easily human-readable format with size units. |
| `_typeof(arg: any): string`<br /><br /> Get expression type. |
| `_timezone_names(): object<IRowsIterator>`<br /><br /> Provide a list of OS time zone names. |
| `_version(): string`<br /><br /> Application version. |
