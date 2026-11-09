using System.Globalization;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal sealed class PluginOptionsGetCommand : BasePluginOptionCommand
{
    /// <inheritdoc />
    public PluginOptionsGetCommand() : base("get", Resources.Messages.PluginOptionsGetCommand_Description)
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateApplicationRootAsync(cancellationToken: cancellationToken);

            var target = await GetOptionAsync(root.Thread, parseResult, cancellationToken: cancellationToken);
            var value = await target.Storage.GetAsync(target.Name, cancellationToken);
            Console.WriteLine(value.ToString(CultureInfo.InvariantCulture));
        });
    }
}
#endif
