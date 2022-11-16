using System.IO;
using System.Threading.Tasks;
using Cake.Common.IO;
using Cake.Compression;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Publish-All")]
[TaskDescription("Publish application for all platforms")]
public class PublishAll : AsyncFrostingTask<BuildContext>
{
    private const int ZipLevel = 9;

    /// <inheritdoc />
    public override async Task RunAsync(BuildContext context)
    {
        var root = Path.Combine(context.OutputDirectory);
        const string licenseFileName = "LICENSE.txt";
        context.CopyFile(Path.Combine(context.OutputDirectory, $"../{licenseFileName}"),
            Path.Combine(context.OutputDirectory, licenseFileName));

        // Linux.
        await new BuildLinuxTask().RunAsync(context);
        context.GZipCompress(
            root,
            Path.Combine(context.OutputDirectory, $"qcat-{context.Version}-{DotNetConstants.RidLinuxX64}.tar.gz"),
            new[]
            {
                Path.Combine(context.OutputDirectory, "qcat"),
                Path.Combine(context.OutputDirectory, "qcat.pdb"),
                Path.Combine(context.OutputDirectory, licenseFileName),
            },
            level: ZipLevel);

        // Windows.
        await new BuildWindowsTask().RunAsync(context);
        context.GZipCompress(
            root,
            Path.Combine(context.OutputDirectory, $"qcat-{context.Version}-{DotNetConstants.RidWindowsX64}.tar.gz"),
            new[]
            {
                Path.Combine(context.OutputDirectory, "qcat.exe"),
                Path.Combine(context.OutputDirectory, "qcat.pdb"),
                Path.Combine(context.OutputDirectory, licenseFileName),
            },
            level: ZipLevel);
    }
}
