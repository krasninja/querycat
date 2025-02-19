using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Linux")]
[TaskDescription("Build project for Linux target")]
public sealed class BuildLinuxTask : BaseBuildTask
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var publishAot = GetPublishAot(context);
        var properties = GetProperties(context);

        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = DotNetConstants.RidLinuxX64,
        });
        context.DotNetPublish(context.PluginsProxyProjectDirectory, new PublishGeneralSettings(context, publishAot: false, properties)
        {
            Runtime = DotNetConstants.RidLinuxX64,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = DotNetConstants.RidLinuxX64,
        });
        context.DotNetPublish(context.TestPluginAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = DotNetConstants.RidLinuxX64,
        });

        return base.RunAsync(context);
    }
}
