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

    public readonly Microsoft.Extensions.Logging.LogLevel GetLogLevel() => Enum.Parse<Microsoft.Extensions.Logging.LogLevel>(
        Marshal.PtrToStringAuto(LogLevel) ?? Sdk.LogLevel.INFORMATION.ToString());

    public readonly ThriftPluginClientArguments ConvertToPluginClientArguments()
        => new()
        {
            ServerEndpoint = GetServerEndpoint(),
            Token = GetToken(),
            LogLevel = GetLogLevel(),
        };
}
