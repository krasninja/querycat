using System;
using System.Runtime.InteropServices;

namespace QueryCat.Plugins.Client;

public delegate void QueryCatPluginMainDelegate(QueryCatPluginArguments args);

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct QueryCatPluginArguments
{
    public IntPtr ServerEndpoint;
    public IntPtr Token;
    public IntPtr LogLevel;

    public readonly string GetServerEndpoint() => Marshal.PtrToStringAuto(ServerEndpoint) ?? string.Empty;

    public readonly string GetToken() => Marshal.PtrToStringAuto(Token) ?? string.Empty;

    public readonly Microsoft.Extensions.Logging.LogLevel GetLogLevel()
        => Enum.TryParse<Microsoft.Extensions.Logging.LogLevel>(
            Marshal.PtrToStringAuto(LogLevel) ?? nameof(Sdk.LogLevel.INFORMATION),
            ignoreCase: true, out var level)
                ? level : Microsoft.Extensions.Logging.LogLevel.Information;

    public readonly ThriftPluginClientArguments ConvertToPluginClientArguments()
        => new()
        {
            ServerEndpoint = GetServerEndpoint(),
            RegistrationToken = GetToken(),
            LogLevel = GetLogLevel(),
        };
}
