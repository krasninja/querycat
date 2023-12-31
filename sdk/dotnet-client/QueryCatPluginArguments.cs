using System;
using System.Runtime.InteropServices;

namespace QueryCat.Plugins.Client;

public delegate void QueryCatPluginMainDelegate(QueryCatPluginArguments args);

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct QueryCatPluginArguments
{
    public IntPtr ServerEndpoint;
    public IntPtr Token;

    public string GetServerEndpoint() => Marshal.PtrToStringAuto(ServerEndpoint) ?? string.Empty;

    public string GetToken() => Marshal.PtrToStringAuto(Token) ?? string.Empty;

    public ThriftPluginClientArguments ConvertToPluginClientArguments()
        => new()
        {
            ServerEndpoint = GetServerEndpoint(),
            Token = GetToken(),
        };
}
