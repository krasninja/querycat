using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Compression;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Publish-All")]
[TaskDescription("Publish application for all platforms")]
public class PublishAll : AsyncFrostingTask<BuildContext>
{
    private const int ZipLevel = 9;

    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var root = Path.Combine(context.OutputDirectory);
        const string licenseFileName = "LICENSE.txt";
        context.CopyFile(Path.Combine(context.OutputDirectory, $"../{licenseFileName}"),
            Path.Combine(context.OutputDirectory, licenseFileName));

        // Linux.
        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidLinuxX64,
        });
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
        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidLinuxArm64,
        });
        context.GZipCompress(
            root,
            Path.Combine(context.OutputDirectory, $"qcat-{context.Version}-{DotNetConstants.RidLinuxArm64}.tar.gz"),
            new[]
            {
                Path.Combine(context.OutputDirectory, "qcat"),
                Path.Combine(context.OutputDirectory, "qcat.pdb"),
                Path.Combine(context.OutputDirectory, licenseFileName),
            },
            level: ZipLevel);

        // Windows.
        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidWindowsX64,
        });
        context.ZipCompress(
            root,
            Path.Combine(context.OutputDirectory, $"qcat-{context.Version}-{DotNetConstants.RidWindowsX64}.zip"),
            new[]
            {
                Path.Combine(context.OutputDirectory, "qcat.exe"),
                Path.Combine(context.OutputDirectory, "qcat.pdb"),
                Path.Combine(context.OutputDirectory, licenseFileName),
            },
            level: ZipLevel);

        // mac OS.
        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidMacOSX64,
        });
        context.GZipCompress(
            root,
            Path.Combine(context.OutputDirectory, $"qcat-{context.Version}-{DotNetConstants.RidMacOSX64}.tar.gz"),
            new[]
            {
                Path.Combine(context.OutputDirectory, "qcat"),
                Path.Combine(context.OutputDirectory, "qcat.pdb"),
                Path.Combine(context.OutputDirectory, licenseFileName),
            },
            level: ZipLevel);
        context.DotNetPublish(context.ConsoleAppProjectDirectory, new PublishGeneralSettings(context)
        {
            OutputDirectory = context.OutputDirectory,
            Runtime = DotNetConstants.RidMacOSXArm64,
        });
        context.GZipCompress(
            root,
            Path.Combine(context.OutputDirectory, $"qcat-{context.Version}-{DotNetConstants.RidMacOSXArm64}.tar.gz"),
            new[]
            {
                Path.Combine(context.OutputDirectory, "qcat"),
                Path.Combine(context.OutputDirectory, "qcat.pdb"),
                Path.Combine(context.OutputDirectory, licenseFileName),
            },
            level: ZipLevel);

        return Task.CompletedTask;
    }
}
