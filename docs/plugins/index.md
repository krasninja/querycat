# Plugins

You can easily extend QueryCat functionality by installing plugins. Plugins allow to add more functions for data processing. There are commands you can use for plugins management:

- `plugin list`. List all local and installed plugins.
- `plugin install <name>`. Install the plugin.
- `plugin remove <name>`. Remove the plugin.
- `plugin update <name>`. Update the plugin. If you provide `*` as name all plugins will be updated.
- `plugin install-proxy`. Download plugin proxy executable from GitHub.
- `plugin auth list`. List authentication providers declared by plugins and login status.
- `plugin auth login <provider>`. Log in to the plugin authentication provider.
- `plugin auth logout <provider>`. Remove stored credentials.

**Note**: Before use, you should install the special plugin proxy using `plugin install-proxy` command. It is the special software needed to run plugin `.dll` files.

Make sure you keep your plugins versions up to date.

## Locations

The plugin files are DLLs or NuGet packages that must contain "Plugin" within name. For example, `QueryCat.Plugins.Logs.nupkg`, `Plugin.AWS.dll` are correct names. The application searches plugins within following locations:

- Local application data directory. For example, `/home/user/.local/share/qcat/plugins/` for Unix or `C:\Users\user\AppData\Local\qcat\plugins\` for Windows.
- Current executable directory.
- Within "plugins" directory of current executable directory.
- With `--plugin-dirs` application argument.

The official (and default) plugin storage can be found here: https://storage.yandexcloud.net/querycat/.

## Authentication

Some plugins need credentials to access remote services (for example, GitHub). Such plugins declare authentication providers, and QueryCat performs login and keeps tokens up to date:

```bash
qcat plugin auth list                        # Show providers and login status.
qcat plugin auth login github                # OAuth2 login, opens browser.
qcat plugin auth login github --device       # OAuth2 device flow, for SSH and headless machines.
qcat plugin auth login github --no-browser   # Print the URL instead of opening browser.
qcat plugin auth login github --api-key      # Enter API key or personal access token instead.
qcat plugin auth logout github
```

The provider can be specified by its identifier or by the plugin name if the plugin declares only one provider. Use `--client-id` and `--client-secret` options to log in with your own registered OAuth2 application.

Tokens are stored in the `auth.json` file within the local application data directory (for example, `/home/user/.local/share/qcat/auth.json`). On Unix the file is readable only by the current user. Expired OAuth2 tokens are refreshed automatically when a plugin requests them. A plugin can only get tokens of the providers it declared.

## Proxy

Proxy contains the full version of .NET platform. Here is the schema how plugins are loaded:

```
QueryCat <-> (Thrift protocol) <-> Proxy <-> (.NET Reflection) <-> Plugin Assembly
```
