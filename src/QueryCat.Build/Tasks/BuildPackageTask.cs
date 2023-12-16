using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Core.IO.Arguments;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Package")]
[TaskDescription("Build NuGet package")]
public sealed class BuildPackageTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        context.DotNetPack(context.BackendProjectDirectory, new DotNetPackSettings
        {
            NoLogo = true,
            OutputDirectory = context.OutputDirectory,
            Configuration = DotNetConstants.ConfigurationRelease,
            // https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets#packing-using-a-nuspec
            ArgumentCustomization = pag =>
            {
                pag.Append(new TextArgument("-p:NuspecFile=QueryCat.nuspec"));
                return pag;
            },
        });

        return Task.CompletedTask;
    }
}
