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
        var platform = GetPlatform(context, DotNetConstants.RidWindowsX64);

        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context, publishAot, properties)
        {
            Runtime = platform,
        });
        context.DotNetPublish(context.PluginsProxyProjectDirectory, new PublishGeneralSettings(context, publishAot: false, properties)
        {
            Runtime = platform,
        });

        return base.RunAsync(context);
    }
}
