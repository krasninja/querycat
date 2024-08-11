using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Core;
using Cake.Core.IO.Arguments;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Assembly-Debug")]
[TaskDescription("Build project to debug assemblies")]
public sealed class BuildAssemblyDebugTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var targetPlatform = context.Arguments.GetArgument("Platform");

        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context, publishAot: false)
        {
            Runtime = targetPlatform,
            ArgumentCustomization = pag =>
            {
                pag.Append(new TextArgument("-p:Plugin=Assembly"));
                return pag;
            }
        });
        context.DeleteFile(Path.Combine(context.OutputDirectory, "qcat-ad"));
        context.MoveFile(
            Path.Combine(context.OutputDirectory, "qcat"),
            Path.Combine(context.OutputDirectory, "qcat-ad"));

        return base.RunAsync(context);
    }
}
