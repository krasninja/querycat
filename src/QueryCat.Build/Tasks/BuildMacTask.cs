using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Mac")]
[TaskDescription("Build project for Mac target")]
public sealed class BuildMacTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context)
        {
            Runtime = DotNetConstants.RidMacOSXArm64,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context)
        {
            Runtime = DotNetConstants.RidMacOSXArm64,
        });
        return Task.CompletedTask;
    }
}
