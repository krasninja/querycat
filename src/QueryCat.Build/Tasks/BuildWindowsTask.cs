using Cake.Common.Tools.DotNet;
using Cake.Core;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Windows")]
[TaskDescription("Build project for Windows target")]
public sealed class BuildWindowsTask : AsyncFrostingTask<BuildContext>
{
    private const bool PublishAotDefault = true;

    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var publishAot = bool.Parse(context.Arguments.GetArgument(DotNetConstants.PublishAotArgument)
            ?? PublishAotDefault.ToString());

        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context, publishAot)
        {
            Runtime = DotNetConstants.RidWindowsX64,
        });
        context.DotNetPublish(context.PluginsProxyProjectDirectory, new PublishGeneralSettings(context, publishAot: false)
        {
            Runtime = DotNetConstants.RidWindowsX64,
        });
        context.DotNetPublish(context.TimeItAppProjectDirectory, new PublishGeneralSettings(context, publishAot)
        {
            Runtime = DotNetConstants.RidWindowsX64,
        });

        return Task.CompletedTask;
    }
}
