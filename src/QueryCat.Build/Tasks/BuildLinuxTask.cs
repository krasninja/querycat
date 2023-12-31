using Cake.Common.Tools.DotNet;
using Cake.Core;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Linux")]
[TaskDescription("Build project for Linux target")]
public sealed class BuildLinuxTask : AsyncFrostingTask<BuildContext>
{
    private const bool PublishAotDefault = true;

    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var publishAot = bool.Parse(context.Arguments.GetArgument(DotNetConstants.PublishAotArgument)
            ?? PublishAotDefault.ToString());

        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context, publishAot)
        {
            Runtime = DotNetConstants.RidLinuxX64,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context, publishAot: true)
        {
            Runtime = DotNetConstants.RidLinuxX64,
        });
        context.DotNetPublish(context.TestPluginAppProjectDirectory, new PublishGeneralSettings(context, publishAot)
        {
            Runtime = DotNetConstants.RidLinuxX64,
        });

        return Task.CompletedTask;
    }
}
