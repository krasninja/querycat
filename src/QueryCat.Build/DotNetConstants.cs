// ReSharper disable InconsistentNaming

using QueryCat.Backend.Core;

namespace QueryCat.Build;

/// <summary>
/// .NET related constants.
/// </summary>
public static class DotNetConstants
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
    public const string RidMacOSX64 = $"{Application.PlatformMacOS}-{Application.ArchitectureX64}";
    public const string RidMacOSArm64 = $"{Application.PlatformMacOS}-{Application.ArchitectureArm64}";

    #endregion

    #region Arguments

    public const string PublishAotArgument = "PublishAot";
    public const string PropertiesArgument = "Properties";

    #endregion
}
