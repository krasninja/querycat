// ReSharper disable InconsistentNaming
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

    public const string RidLinuxX64 = "linux-x64";
    public const string RidLinuxArm64 = "linux-arm64";
    public const string RidLinuxMuslX64 = "linux-musl-x64";
    public const string RidWindowsX64 = "win-x64";
    public const string RidMacOSX64 = "osx.12-x64";
    public const string RidMacOSXArm64 = "osx.12-arm64";

    #endregion
}
