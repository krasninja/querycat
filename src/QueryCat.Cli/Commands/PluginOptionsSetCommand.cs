using System.CommandLine;
using System.Globalization;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal sealed class PluginOptionsSetCommand : BasePluginOptionCommand
{
    /// <inheritdoc />
    public PluginOptionsSetCommand() : base("set", Resources.Messages.PluginOptionsSetCommand_Description)
    {
        var valueArgument = new Argument<string>("value")
        {
            Description = Resources.Messages.PluginOptionsSetCommand_ValueDescription,
        };

        this.Add(valueArgument);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateApplicationRootAsync(cancellationToken: cancellationToken);

            var target = await GetOptionAsync(root.Thread, parseResult, allowUndeclared: true, cancellationToken);
            if (target.Option == null)
            {
                var logger = Application.LoggerFactory.CreateLogger(nameof(PluginOptionsSetCommand));
                logger.LogWarning("Option '{Option}' is not declared by plugin '{Plugin}'.",
                    target.Name, target.Storage.PluginName);
            }
            await target.Storage.SetAsync(target.Name, new VariantValue(parseResult.GetRequiredValue(valueArgument)),
                cancellationToken);
            await target.Storage.SaveAsync(cancellationToken);

            var value = await target.Storage.GetAsync(target.Name, cancellationToken);
            Console.WriteLine(Resources.Messages.PluginOptionsSet_Success, target.Key,
                value.ToString(CultureInfo.InvariantCulture));
        });
    }
}
#endif
