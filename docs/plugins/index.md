# Plugins

You can easily extend QueryCat functionality by installing plugins. Plugins allow to add more functions for data processing. There are commands you can use for plugins management:

- `plugin list`. List all local and installed plugins.
- `plugin install <name>`. Install the plugin.
- `plugin remove <name>`. Remove the plugin.
- `plugin update <name>`. Update the plugin. If you provide `*` as name all plugins will be updated.
- `plugin install-proxy`. Download plugin proxy executable from GitHub.

**Note**: Before use, your should install the special plugin proxy using `plugin install-proxy` command. It is the special software needed to run plugin `.dll` files.

Make sure you keep you plugins versions up to date.

## Locations

The plugin files are DLLs or NuGet packages that must contain "Plugin" within name. For example, `QueryCat.Plugins.Logs.nupkg`, `Plugin.AWS.dll` are correct names. The application searchs plugins within following locations:

- Local application data directory. For example, `/home/user/.local/share/qcat/plugins/` for Unix or `C:\Users\user\AppData\Local\qcat\plugins\` for Windows.
- Current executable directory.
- Within "plugins" directory of current executable directory.
- With `--plugin-dirs` application argument.

The official (and default) plugin storage can be found here: https://storage.yandexcloud.net/querycat/.

## Proxy

Proxy contains the full version of .NET platform. Here is the schema how plugins are loaded:

```
QueryCat <-> (Thrift protocol) <-> Proxy <-> (.NET Reflection) <-> Plugin Assembly
```
