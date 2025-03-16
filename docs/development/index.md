# Development

QueryCat allows to develop custom input formats and functions. Right now, only .NET libraries are supported. You can use any language that supports .NET IL code compilation.

Read the following sections for better internals understanding:

1. [Components](components.md). General information about main classes and abstractions.
2. [Functions](functions.md). The basics about QueryCat functions.
3. [.NET Assembly Connection](connector-assembly.md). Implement your plugin as .NET assembly.
4. [Thrift Connector](connector-thrift.md). Implement your plugin as standalone application.
5. [Simple Plugin](plugin-simple.md). How to create simple plugin.
6. [Advanced Plugin](plugin-advanced.md). Other ways to define plugin.
7. [Build Tasks](build-tasks.md). Available build tasks.
8. [SDK](sdk.md). NuGet package.

## Plugin Search

By default the QueryCat CLI searches the plugins files within following directories:

1. The current OS application directory. On Linux systems it looks like this: `/home/ivan/.local/share/qcat/plugins`. On Windows systems it is like TODO.
2. The directory of the `qcat` executable.
3. The directory `plugins` within the `qcat` executable.
4. The directories specified by `--plugin-dirs` command line argument.

Once observed the QueryCat tries to register it observing all functions within it.
