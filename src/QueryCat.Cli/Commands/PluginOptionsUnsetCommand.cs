using QueryCat.Backend.Core.Types;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal sealed class PluginOptionsUnsetCommand : BasePluginOptionCommand
{
    /// <inheritdoc />
    public PluginOptionsUnsetCommand() : base("unset", Resources.Messages.PluginOptionsUnsetCommand_Description)
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateApplicationRootAsync(cancellationToken: cancellationToken);

            var target = await GetOptionAsync(root.Thread, parseResult, cancellationToken: cancellationToken);
            await target.Storage.SetAsync(target.Name, VariantValue.Null, cancellationToken);
            await target.Storage.SaveAsync(cancellationToken);
            Console.WriteLine(Resources.Messages.PluginOptionsUnset_Success, target.Key);
        });
    }
}
#endif
