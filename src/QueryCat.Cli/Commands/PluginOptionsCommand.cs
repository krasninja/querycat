using System.CommandLine;
using System.Globalization;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
internal sealed class PluginOptionsCommand : BaseCommand
{
    private sealed record PluginOptionStatus(
        string Plugin,
        string Name,
        string Type,
        string Key,
        string DefaultValue,
        string Value,
        string Description);

    /// <inheritdoc />
    public PluginOptionsCommand() : base("options", Resources.Messages.PluginOptionsCommand_Description)
    {
        var pluginArgument = new Argument<string>("plugin")
        {
            Description = Resources.Messages.PluginOptionsCommand_PluginDescription,
            Arity = ArgumentArity.ZeroOrOne,
        };

        this.Add(pluginArgument);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateStdoutApplicationRootAsync(
                columnsSeparator: parseResult.GetValue(ColumnsSeparatorOption),
                outputStyle: parseResult.GetValue(OutputStyleOption),
                cancellationToken: cancellationToken);

            var options = await root.Thread.PluginsManager.GetOptionsAsync(
                parseResult.GetValue(pluginArgument) ?? string.Empty, cancellationToken);
            var items = new List<PluginOptionStatus>(options.Count);
            foreach (var option in options)
            {
                var value = await root.Thread.ConfigStorage.GetAsync(option.Key, cancellationToken);
                items.Add(new PluginOptionStatus(
                    option.PluginName,
                    option.Name,
                    option.Type.ToString(),
                    option.Key,
                    option.DefaultValue.ToString(CultureInfo.InvariantCulture),
                    value.ToString(CultureInfo.InvariantCulture),
                    option.Description));
            }

            var input = EnumerableRowsInput<PluginOptionStatus>.FromSource(items,
                builder => builder
                    .AddProperty("plugin", p => p.Plugin, "Plugin name.")
                    .AddProperty("name", p => p.Name, "Option name.")
                    .AddProperty("type", p => p.Type, "Option value type.")
                    .AddProperty("key", p => p.Key, "Config storage key.")
                    .AddProperty("default", p => p.DefaultValue, "Default value.")
                    .AddProperty("value", p => p.Value, "Current value.")
                    .AddProperty("description", p => p.Description, "Option description.")
            );
            await WriteAsync(root.Thread, VariantValue.CreateFromObject(input), root.RowsOutput, cancellationToken);
        });
    }
}
#endif
