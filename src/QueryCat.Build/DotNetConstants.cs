// ReSharper disable InconsistentNaming

using QueryCat.Backend.Core;

namespace QueryCat.Build;

/// <summary>
/// .NET related constants.
/// </summary>
internal static class DotNetConstants
{
    #region Target

    public const string ConfigurationRelease = "Release";
    public const string ConfigurationDebug = "Debug";

    #endregion

    #region .NET RID Catalog

    // https://docs.microsoft.com/en-us/dotnet/core/rid-catalog.

    public const string RidLinuxX64 = $"{Application.PlatformLinux}-{Application.ArchitectureX64}";
    public const string RidLinuxArm64 = $"{Application.PlatformLinux}-{Application.ArchitectureArm64}";
    public const string RidLinuxMuslX64 = $"{Application.PlatformLinux}-musl-{Application.ArchitectureX64}";
    public const string RidWindowsX64 = $"{Application.PlatformWindows}-{Application.ArchitectureX64}";
    public const string RidMacOSX64 = $"osx.12-{Application.ArchitectureX64}";
    public const string RidMacOSXArm64 = $"osx.12-{Application.ArchitectureArm64}";

    #endregion

    #region Arguments

    public const string PublishAotArgument = "PublishAot";

    #endregion
}
