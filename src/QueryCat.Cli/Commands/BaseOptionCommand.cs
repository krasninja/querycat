using System.CommandLine;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Cli.Commands;

/// <summary>
/// Base command to work with the specific configuration option.
/// </summary>
internal abstract class BaseOptionCommand : BaseCommand
{
    protected Argument<string> NameArgument { get; } = new("name")
    {
        Description = Resources.Messages.PluginOptionsCommand_NameDescription,
    };

    /// <inheritdoc />
    protected BaseOptionCommand(string name, string? description = null) : base(name, description)
    {
        Add(NameArgument);
    }

    /// <summary>
    /// Target configuration option.
    /// </summary>
    /// <param name="Name">Option name. It is also the config storage key.</param>
    /// <param name="Option">Declared option or null if the option is not declared by any plugin.</param>
    /// <param name="Storage">Config storage.</param>
    protected sealed record OptionTarget(string Name, PluginConfigOption? Option, IConfigStorage Storage)
    {
        /// <summary>
        /// Get the option value. The default value is returned if the option is not set.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Option value.</returns>
        public async ValueTask<VariantValue> GetAsync(CancellationToken cancellationToken = default)
        {
            var value = await Storage.GetAsync(Name, cancellationToken);
            return value.IsNull && Option != null ? Option.DefaultValue : value;
        }

        /// <summary>
        /// Set the option value. For the declared option the value is converted to the option type.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public ValueTask SetAsync(VariantValue value, CancellationToken cancellationToken = default)
        {
            if (Option != null && !value.IsNull)
            {
                value = value.Cast(Option.Type);
            }
            return Storage.SetAsync(Name, value, cancellationToken);
        }
    }

    /// <summary>
    /// Find the configuration option.
    /// </summary>
    /// <param name="thread">Execution thread.</param>
    /// <param name="parseResult">Parse result.</param>
    /// <param name="allowUndeclared">Allow options that are not declared by plugins.
    /// Otherwise, the exception is thrown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Option target.</returns>
    protected async Task<OptionTarget> GetOptionAsync(
        IExecutionThread thread,
        ParseResult parseResult,
        bool allowUndeclared = false,
        CancellationToken cancellationToken = default)
    {
        var optionName = parseResult.GetRequiredValue(NameArgument);

        // The config is loaded lazily on the first query run, so load it explicitly. Otherwise,
        // the save would overwrite the config file with the empty storage.
        await thread.ConfigStorage.LoadAsync(cancellationToken);

        var options = await thread.PluginsManager.GetOptionsAsync(string.Empty, cancellationToken);
        var option = options.FirstOrDefault(o => o.Name.Equals(optionName, StringComparison.OrdinalIgnoreCase));
        if (option != null)
        {
            return new OptionTarget(option.Name, option, thread.ConfigStorage);
        }

        if (!allowUndeclared)
        {
            throw new QueryCatException(string.Format(Resources.Errors.PluginOptionNotFound, optionName));
        }
        return new OptionTarget(optionName, null, thread.ConfigStorage);
    }
}
