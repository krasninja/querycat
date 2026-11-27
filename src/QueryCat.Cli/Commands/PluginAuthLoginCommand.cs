using System.CommandLine;
using System.Text;
using QueryCat.Backend.Core.Plugins.Auth;
using QueryCat.OAuth;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal sealed class PluginAuthLoginCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginAuthLoginCommand() : base("login", Resources.Messages.PluginAuthLoginCommand_Description)
    {
        var providerArgument = new Argument<string>("provider")
        {
            Description = Resources.Messages.PluginAuthCommand_ProviderDescription,
        };
        var deviceOption = new Option<bool>("--device")
        {
            Description = Resources.Messages.PluginAuthLoginCommand_DeviceDescription,
        };
        var noBrowserOption = new Option<bool>("--no-browser")
        {
            Description = Resources.Messages.PluginAuthLoginCommand_NoBrowserDescription,
        };
        var apiKeyOption = new Option<bool>("--api-key")
        {
            Description = Resources.Messages.PluginAuthLoginCommand_ApiKeyDescription,
        };
        var clientIdOption = new Option<string>("--client-id")
        {
            Description = Resources.Messages.PluginAuthLoginCommand_ClientIdDescription,
        };
        var clientSecretOption = new Option<string>("--client-secret")
        {
            Description = Resources.Messages.PluginAuthLoginCommand_ClientSecretDescription,
        };

        this.Add(providerArgument);
        this.Add(deviceOption);
        this.Add(noBrowserOption);
        this.Add(apiKeyOption);
        this.Add(clientIdOption);
        this.Add(clientSecretOption);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateApplicationRootAsync(cancellationToken: cancellationToken);

            var authService = new AuthService(root.Thread.AuthTokensManager.Providers, root.AuthTokenStore);
            var options = new AuthLoginOptions
            {
                UseDeviceFlow = parseResult.GetValue(deviceOption),
                UseApiKey = parseResult.GetValue(apiKeyOption),
                OpenBrowser = !parseResult.GetValue(noBrowserOption),
                ClientId = parseResult.GetValue(clientIdOption),
                ClientSecret = parseResult.GetValue(clientSecretOption),
            };
            var provider = await authService.LoginAsync(parseResult.GetRequiredValue(providerArgument), options,
                new ConsoleAuthLoginInteraction(), cancellationToken);
            Console.WriteLine(Resources.Messages.PluginAuthLogin_Success, provider.Id);
        });
    }

    private sealed class ConsoleAuthLoginInteraction : IAuthLoginInteraction
    {
        /// <inheritdoc />
        public ValueTask<string> ReadApiKeyAsync(AuthProviderInfo provider, CancellationToken cancellationToken)
            => ValueTask.FromResult(ReadSecret(string.Format(Resources.Messages.PluginAuthLogin_EnterApiKey, provider.Id)));

        /// <inheritdoc />
        public void ShowAuthorizationUrl(string url) => Console.WriteLine(Resources.Messages.PluginAuthLogin_OpenUrl, url);

        /// <inheritdoc />
        public void ShowDeviceCode(string verificationUri, string userCode)
            => Console.WriteLine(Resources.Messages.PluginAuthLogin_DeviceCode, verificationUri, userCode);

        /// <inheritdoc />
        public void ShowWaiting() => Console.WriteLine(Resources.Messages.PluginAuthLogin_Waiting);

        private static string ReadSecret(string prompt)
        {
            Console.Write(prompt);
            if (Console.IsInputRedirected)
            {
                return Console.In.ReadLine() ?? string.Empty;
            }

            var sb = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(intercept: true);
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Length--;
                    }
                    continue;
                }
                if (!char.IsControl(key.KeyChar))
                {
                    sb.Append(key.KeyChar);
                }
            }
            Console.WriteLine();
            return sb.ToString();
        }
    }
}
#endif
