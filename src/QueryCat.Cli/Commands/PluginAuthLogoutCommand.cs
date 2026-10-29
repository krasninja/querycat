using System.CommandLine;
using QueryCat.OAuth;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal sealed class PluginAuthLogoutCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginAuthLogoutCommand() : base("logout", Resources.Messages.PluginAuthLogoutCommand_Description)
    {
        var providerArgument = new Argument<string>("provider")
        {
            Description = Resources.Messages.PluginAuthCommand_ProviderDescription,
        };

        this.Add(providerArgument);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateApplicationRootAsync(cancellationToken: cancellationToken);

            var authService = new AuthService(root.Thread.AuthTokensManager.Providers, root.AuthTokenStore);
            var (providerId, removed) = await authService.LogoutAsync(
                parseResult.GetRequiredValue(providerArgument), cancellationToken);
            Console.WriteLine(removed ? Resources.Messages.PluginAuthLogout_Success : Resources.Messages.PluginAuthLogout_NotFound,
                providerId);
        });
    }
}
#endif
