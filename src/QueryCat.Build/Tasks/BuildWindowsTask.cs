using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Windows")]
[TaskDescription("Build project for Windows target")]
public sealed class BuildWindowsTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidWindowsX64,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidWindowsX64,
        });
        return Task.CompletedTask;
    }
}
