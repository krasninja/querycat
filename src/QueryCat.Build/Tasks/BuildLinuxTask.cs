using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Linux")]
[TaskDescription("Build project for Linux target")]
public sealed class BuildLinuxTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidLinuxX64,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidLinuxX64,
        });
        context.DotNetPublish(context.TestPluginAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidLinuxX64,
            PublishTrimmed = false,
            PublishSingleFile = true,
        });
        return Task.CompletedTask;
    }
}
