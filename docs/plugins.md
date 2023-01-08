# Plugins

You can easily extend QueryCat functionality by installing plugins. Plugins allow to add more functions for data processing. There are commands you can use for plugins management:

- `plugin list`. List all local and installed plugins.
- `plugin install <name>`. Install the plugin.
- `plugin remove <name>`. Remove the plugin.
- `plugin update <name>`. Update the plugin. If you provide `*` as name all plugins will be updated.

Make sure you keep you plugins versions up to date.

Here is the list of available plugins.

| Name | Description |
| --- | --- |
| `QueryCat.Plugins.Logs` | Various functions to parse log files (IISW3C). |

## Locations

The plugin files are DLLs or NuGet packages that must contain "Plugin" within name. For example, `QueryCat.Plugins.Logs.nupkg`, `Plugin.AWS.dll` are correct names. The application searchs plugins within following locations:

- Local application data directory. For example, `/home/ivan/.local/share/qcat/plugins/` for Unix or `C:\Users\ivan\AppData\Local\qcat\plugins\` for Windows.
- Current executable directory.
- Within "plugins" directory of current executable directory.
- With `--plugin-dirs` application argument.
