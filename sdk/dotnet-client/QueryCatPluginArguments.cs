using System;
using System.Runtime.InteropServices;

namespace QueryCat.Plugins.Client;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct QueryCatPluginArguments
{
    public IntPtr ServerPipeName;
    public IntPtr Token;
    public int ParentPid;

    public string GetServerPipeName() => Marshal.PtrToStringAuto(ServerPipeName) ?? string.Empty;

    public string GetToken() => Marshal.PtrToStringAuto(Token) ?? string.Empty;

    public ThriftPluginClientArguments ConvertToPluginClientArguments()
        => new()
        {
            ServerPipe = GetServerPipeName(),
            Token = GetToken(),
            ParentPid = ParentPid,
        };
}

public delegate void QueryCatPluginMainDelegate(QueryCatPluginArguments args);
