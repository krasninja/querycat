using Cake.Common.Tools.DotNet;
using Cake.Core;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Mac")]
[TaskDescription("Build project for Mac target")]
public sealed class BuildMacTask : AsyncFrostingTask<BuildContext>
{
    private const bool PublishAotDefault = true;

    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var publishAot = bool.Parse(context.Arguments.GetArgument(DotNetConstants.PublishAotArgument)
            ?? PublishAotDefault.ToString());

        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context, publishAot)
        {
            Runtime = DotNetConstants.RidMacOSXArm64,
        });
        context.DotNetPublish(context.PluginsProxyProjectDirectory, new PublishGeneralSettings(context, publishAot: false)
        {
            Runtime = DotNetConstants.RidMacOSXArm64,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context, publishAot)
        {
            Runtime = DotNetConstants.RidMacOSXArm64,
        });
        return Task.CompletedTask;
    }
}
