using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Mac")]
[TaskDescription("Build project for Mac target")]
public sealed class BuildMacTask : BaseBuildTask
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var publishAot = GetPublishAot(context);
        var properties = GetProperties(context);

        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = DotNetConstants.RidMacOSXArm64,
        });
        context.DotNetPublish(context.PluginsProxyProjectDirectory, new PublishGeneralSettings(context, publishAot: false, properties)
        {
            Runtime = DotNetConstants.RidMacOSXArm64,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = DotNetConstants.RidMacOSXArm64,
        });

        return base.RunAsync(context);
    }
}
