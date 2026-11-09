# Plugin Configuration

Plugins can store settings in the QueryCat host config storage (`config.json` in the application directory). Every plugin has its own namespace: the host prefixes all keys with the plugin name, so a plugin cannot read or overwrite keys of another plugin.

```
plugin: ConfigStorage.SetAsync("url", "https://github.com")  ->  config.json: "QueryCat.Plugins.Github:url"
```

## Declare Options

Plugins can declare configuration options. Declared options have a type, a description and a default value. Add the `RegisterConfigOptions` method to the `Registration` class. It is discovered the same way as `RegisterFunctions`, so it works for plugins loaded by the proxy and by the assembly plugins loader.

```csharp
public static class Registration
{
    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        // ...
    }

    public static void RegisterConfigOptions(IPluginConfigOptionsRegistry registry)
    {
        registry.Register(new PluginConfigOption("url", DataType.String, "GitHub API URL.",
            new VariantValue("https://api.github.com")));
        registry.Register(new PluginConfigOption("page_size", DataType.Integer, "Number of items per request.",
            new VariantValue(100)));
    }
}
```

If the plugin has its own entry point (`ThriftPluginClient`), register options before `StartAsync`:

```csharp
Registration.RegisterFunctions(client.FunctionsManager);
Registration.RegisterConfigOptions(client.ConfigOptions);
await client.StartAsync(cancellationToken: ct);
```

For declared options the host:

- Converts the value to the option type on set. If the value cannot be converted, an error is thrown. Use `DataType.Dynamic` to skip the conversion.
- Returns the default value if the option is not set.

Undeclared keys can still be used (f.e. to keep the plugin state). They are prefixed with the plugin name too.

## Read Options

Use `QueryContext.ConfigStorage` within rows input or output, or `IExecutionThread.ConfigStorage` within the function:

```csharp
var url = (await QueryContext.ConfigStorage.GetAsync("url", cancellationToken)).AsString;
```

## Manage Options

Declared options are stored in the plugins cache, so they can be listed without starting the plugin. Users can manage option values with `qcat plugin options` commands. The plugin can be specified by its short name:

```
qcat plugin options list
qcat plugin options list github
qcat plugin options get github url
qcat plugin options set github url https://github.example.com/api
qcat plugin options unset github url
```

The value of the declared option is converted to the option type the same way as it is done for the plugin. `set` also accepts undeclared options: the value is stored as a string and a warning is logged. `get` and `unset` work with declared options only.

## Limitations

The keys prefixing is done by the host for plugins that run out-of-process (Thrift plugins and plugins loaded by the proxy). Plugins loaded in-process by `DotNetAssemblyPluginsLoader` (`Plugin=Assembly` build) share the host config storage directly, so their keys are not prefixed.
