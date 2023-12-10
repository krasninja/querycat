namespace QueryCat.Plugins.Client;

public sealed class ThriftPluginClientArguments
{
    public string ServerEndpoint { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public int ParentPid { get; set; }

    public string DebugServerPath { get; set; } = string.Empty;

    public string DebugServerPathArgs { get; set; } = string.Empty;
}
