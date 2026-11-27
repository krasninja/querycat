using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Types;
using QueryCat.OAuth;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal sealed class PluginAuthListCommand : BaseCommand
{
    /// <inheritdoc />
    public PluginAuthListCommand() : base("list", Resources.Messages.PluginAuthListCommand_Description)
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateStdoutApplicationRootAsync(
                columnsSeparator: parseResult.GetValue(ColumnsSeparatorOption),
                outputStyle: parseResult.GetValue(OutputStyleOption),
                cancellationToken: cancellationToken);

            var authService = new AuthService(root.Thread.AuthTokensManager.Providers, root.AuthTokenStore);
            var items = await authService.GetStatusesAsync(cancellationToken);

            var input = EnumerableRowsInput<AuthProviderStatus>.FromSource(items,
                builder => builder
                    .AddProperty("id", p => p.Id, "Authentication provider identifier.")
                    .AddProperty("plugin", p => p.Plugin, "Plugin name.")
                    .AddProperty("type", p => p.ProviderType, "Supported authentication types.")
                    .AddProperty("status", p => p.Status, "Login status.")
                    .AddProperty("token_type", p => p.TokenType, "Stored credentials type.")
                    .AddProperty("expires_at", p => p.ExpiresAt, "Access token expiration time.")
                    .AddProperty("scopes", p => p.Scopes, "Granted scopes.")
            );
            await WriteAsync(root.Thread, VariantValue.CreateFromObject(input), root.RowsOutput, cancellationToken);
        });
    }
}
#endif
