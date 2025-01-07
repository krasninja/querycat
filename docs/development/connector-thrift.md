# Thrift

The QueryCat plugin can be written as a separate executable. The interaction between QueryCat host and plugin is implemented using Apache Thrift protocol.

[+] Any language can be used for development.
[-] Might be harder to develop.
[-] In some cases the performance can be low.

## Technical Information

- protocol: binary;
- transport: named pipes on Windows and sockets on Linux;

The QueryCat host starts your plugin with the following command line arguments:

- `--server-pipe-name`. The address of socket or names pipe channel.
- `--token`. The special auth token that should be provided on registration. It is needed to prevent unauthorized access.
- `--parent-pid`. The QueryCat process identifier.

## Guide

The guide below describes how to write a plugin.

### .NET Approach

1. Create a new empty console application project.

    ```bash
    dotnet new console --name SimplePlugin
    ```

    **NOTE:** The plugin must contain "Plugin" (case insensitive) word in its name.

2. Reference `QueryCat.Plugins.Client` project. Right now it is not available in NuGet, you can clone the repository somewhere and reference it. For example:

    ```
    <ItemGroup>
        <ProjectReference Include="..\querycat\src\QueryCat.Plugins.Client\QueryCat.Plugins.Client.csproj" />
    </ItemGroup>
    ```

3. Implement functions and rows inputs.

4. Register them within `Program.cs`, for example:

    ```csharp
    public class Program
    {
        internal static async Task Main(string[] args)
        {
            ThriftPluginClient.SetupApplicationLogging();
            using var client = new ThriftPluginClient(ThriftPluginClient.ConvertCommandLineArguments(args));
            client.FunctionsManager.RegisterFunction(TestFunctions.TestCombineFunction);
            await client.StartAsync();
            await client.WaitForServerExitAsync();
        }
    }
    ```

5. Compile your application into `.exe` file:

    ```bash
    dotnet build -c:Release -r linux-x64 -p:PublishSingleFile=true
    ```

For debug, you can run your plugin with following arguments:

```
--debug-server="/opt/qcat"
--debug-server-file="/home/ivan/github.sql"
```

or

```
--debug-server="/opt/qcat"
--debug-server-query="select 'some SQL';"
```

It will enforce QueryCat to run your plugin with the specific query.

### Other Languages Approach

Use the following Thrift file to generate a proxy: https://github.com/krasninja/querycat/blob/develop/sdk/QueryCat.thrift .

1. Handle command line arguments to properly connect to QueryCat host.

2. Call the `RegisterPlugin` function of `PluginsManager` service.

## Plugin Registration Process

1. Once observed, the QueryCat host starts temporary registration server (using named pipes on Windows or Unix socket) and runs the plugin executable with the following arguments: `server-pipe-name`, `token`, `parent-pid`. See above for additional information.

2. The plugin should connect to the host and call `RegisterPlugin` method. It should be done within 10 seconds.

3. The host keeps the connection and can call plugins' functions using `CallFunction` method.

## Debug Mode

To debug your plugins easier, the QueryCat has a special command. It will call your plugin in a special test mode.

```bash
qcat plugin debug
```

In that mode the server pipe name will always be `qcat-test` and token will be `test`.
