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
        var platform = GetPlatform(context, DotNetConstants.RidLinuxX64);

        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = platform,
        });
        context.DotNetPublish(context.PluginsProxyProjectDirectory, new PublishGeneralSettings(context, publishAot: false, properties)
        {
            Runtime = platform,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = platform,
        });
        context.DotNetPublish(context.TestPluginAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = platform,
        });

        return base.RunAsync(context);
    }
}
