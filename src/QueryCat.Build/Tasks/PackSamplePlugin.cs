using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Core.IO.Arguments;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Pack-Sample-Plugin")]
public sealed class PackSamplePlugin : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var targetProject = @"QueryCat.Plugins.Sample";

        context.DotNetPack(Path.Combine("..", targetProject), new DotNetPackSettings
        {
            OutputDirectory = context.OutputDirectory,
            Configuration = DotNetConstants.ConfigurationRelease,
            NoLogo = true,
            ArgumentCustomization = pag =>
            {
                pag.Append(new TextArgument("-p:UseAssemblyName=false"));
                pag.Append(new TextArgument("-p:OutputType=Library"));
                return pag;
            },
        });

        return Task.CompletedTask;
    }
}
