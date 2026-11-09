using System.CommandLine;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Plugins;

namespace QueryCat.Cli.Commands;

#if ENABLE_PLUGINS
/// <summary>
/// Base command to work with the specific plugin configuration option.
/// </summary>
internal abstract class BasePluginOptionCommand : BaseCommand
{
    protected Argument<string> PluginArgument { get; } = new("plugin")
    {
        Description = Resources.Messages.PluginOptionsCommand_PluginDescription,
    };

    protected Argument<string> NameArgument { get; } = new("name")
    {
        Description = Resources.Messages.PluginOptionsCommand_NameDescription,
    };

    /// <inheritdoc />
    protected BasePluginOptionCommand(string name, string? description = null) : base(name, description)
    {
        Add(PluginArgument);
        Add(NameArgument);
    }

    /// <summary>
    /// Target plugin option.
    /// </summary>
    /// <param name="Name">Option name.</param>
    /// <param name="Option">Declared option or null if the option is not declared by the plugin.</param>
    /// <param name="Storage">Config storage scoped to the plugin.</param>
    protected sealed record PluginOptionTarget(string Name, PluginConfigOption? Option, PluginConfigStorage Storage)
    {
        /// <summary>
        /// The config storage key.
        /// </summary>
        public string Key => PluginConfigOption.GetKey(Storage.PluginName, Name);
    }

    /// <summary>
    /// Find the plugin option and create the config storage scoped to its plugin.
    /// </summary>
    /// <param name="thread">Execution thread.</param>
    /// <param name="parseResult">Parse result.</param>
    /// <param name="allowUndeclared">Allow options that are not declared by the plugin.
    /// Otherwise, the exception is thrown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Plugin option target.</returns>
    protected async Task<PluginOptionTarget> GetOptionAsync(
        IExecutionThread thread,
        ParseResult parseResult,
        bool allowUndeclared = false,
        CancellationToken cancellationToken = default)
    {
        var pluginName = parseResult.GetRequiredValue(PluginArgument);
        var optionName = parseResult.GetRequiredValue(NameArgument);

        // The config is loaded lazily on the first query run, so load it explicitly. Otherwise,
        // the save would overwrite the config file with the empty storage.
        await thread.ConfigStorage.LoadAsync(cancellationToken);

        var options = await thread.PluginsManager.GetOptionsAsync(pluginName, cancellationToken);
        var option = options.FirstOrDefault(o => o.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase));

        // Use the plugin scoped storage, so the value is converted the same way as it is done for the plugin.
        var registry = new PluginConfigOptionsRegistry();
        if (option != null)
        {
            registry.Register(option);
            return new PluginOptionTarget(option.Name, option,
                new PluginConfigStorage(thread.ConfigStorage, option.PluginName, registry));
        }

        if (!allowUndeclared)
        {
            throw new QueryCatException(string.Format(Resources.Errors.PluginOptionNotFound, optionName, pluginName));
        }
        var fullPluginName = options.FirstOrDefault()?.PluginName
            ?? (await thread.PluginsManager.ListAsync(
                    new PluginListOptions { LocalOnly = true, Name = pluginName }, cancellationToken))
                .FirstOrDefault()?.Name
            ?? throw new QueryCatException(string.Format(Resources.Errors.PluginNotFound, pluginName));
        return new PluginOptionTarget(optionName, null,
            new PluginConfigStorage(thread.ConfigStorage, fullPluginName, registry));
    }
}
#endif
