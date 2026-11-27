# Information Functions

The various information functions about data source and application. The information functions have `_` prefix.

| Name and Description |
| --- |
| `_functions(): object<IRowsInput>`<br /><br /> Returns all registered functions. |
| `_function_args(name: string): object<IRowsInput>`<br /><br /> Returns function arguments. |
| `_platform(): string`<br /><br /> Gets current running platform/OS. |
| `_plugins(): object<IRowsInput>`<br /><br /> Returns available plugins from repository. |
| `_schema(input: object<IRowsInput>): object<IRowsInput>`<br /><br /> Returns row input columns information. |
| `_typeof(arg: any): string`<br /><br /> Gets expression type. |
| `_timezone_names(): object<IRowsInput>`<br /><br /> Provides a list of OS time zone names. |
| `_version(): string`<br /><br /> Application version. |
