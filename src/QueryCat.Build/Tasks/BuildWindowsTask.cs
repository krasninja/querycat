using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Windows")]
[TaskDescription("Build project for Windows target")]
public sealed class BuildWindowsTask : BaseBuildTask
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var publishAot = GetPublishAot(context);
        var properties = GetProperties(context);

        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = DotNetConstants.RidWindowsX64,
        });
        context.DotNetPublish(context.PluginsProxyProjectDirectory, new PublishGeneralSettings(context, publishAot: false, properties)
        {
            Runtime = DotNetConstants.RidWindowsX64,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = DotNetConstants.RidWindowsX64,
        });

        return base.RunAsync(context);
    }
}
