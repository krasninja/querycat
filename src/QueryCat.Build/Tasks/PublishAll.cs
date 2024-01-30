using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Compression;
using Cake.Core;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Publish-All")]
[TaskDescription("Publish application for all platforms")]
public class PublishAll : AsyncFrostingTask<BuildContext>
{
    private const int ZipLevel = 9;
    private const string LicenseFileName = "LICENSE.txt";

    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var targetPlatform = context.Arguments.GetArgument("Platform");
        var root = Path.Combine(context.OutputDirectory);
        context.CopyFile(Path.Combine(context.OutputDirectory, $"../{LicenseFileName}"),
            Path.Combine(context.OutputDirectory, LicenseFileName));

        if (targetPlatform.Contains("linux", StringComparison.OrdinalIgnoreCase))
        {
            PublishLinux(context, root);
        }
        if (targetPlatform.Contains("win", StringComparison.OrdinalIgnoreCase))
        {
            PublishWindows(context, root);
        }
        if (targetPlatform.Contains("mac", StringComparison.OrdinalIgnoreCase))
        {
            PublishMacOs(context, root);
        }

        return Task.CompletedTask;
    }

    private static void PublishLinux(BuildContext context, string root)
    {
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
                Path.Combine(context.OutputDirectory, LicenseFileName),
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
                Path.Combine(context.OutputDirectory, LicenseFileName),
            },
            level: ZipLevel);
    }

    private static void PublishWindows(BuildContext context, string root)
    {
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
                Path.Combine(context.OutputDirectory, LicenseFileName),
            },
            level: ZipLevel);
    }

    private static void PublishMacOs(BuildContext context, string root)
    {
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
                Path.Combine(context.OutputDirectory, LicenseFileName),
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
                Path.Combine(context.OutputDirectory, LicenseFileName),
            },
            level: ZipLevel);
    }
}
