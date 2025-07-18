using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Clean")]
[TaskDescription("Clean up output, bin and obj directories")]
public sealed class CleanTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        // Dotnet clean with Release configuration.
        context.DotNetClean(context.ConsoleAppProjectDirectory, new DotNetCleanSettings
        {
            Configuration = DotNetConstants.ConfigurationRelease,
        });
        context.DotNetClean(context.TestPluginAppProjectDirectory, new DotNetCleanSettings
        {
            Configuration = DotNetConstants.ConfigurationRelease,
        });

        // Dotnet clean with Debug configuration.
        context.DotNetClean(context.ConsoleAppProjectDirectory, new DotNetCleanSettings
        {
            Configuration = DotNetConstants.ConfigurationDebug,
        });
        context.DotNetClean(context.TestPluginAppProjectDirectory, new DotNetCleanSettings
        {
            Configuration = DotNetConstants.ConfigurationDebug,
        });

        // TODO:
        return base.RunAsync(context);
    }
}
