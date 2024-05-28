using Microsoft.Extensions.Logging;

namespace QueryCat.Plugins.Client;

public sealed class ThriftPluginClientArguments
{
    public string ServerEndpoint { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public int ParentPid { get; set; }

    public string DebugServerPath { get; set; } = string.Empty;

    public string DebugServerQueryText { get; set; } = string.Empty;

    public string DebugServerQueryFile { get; set; } = string.Empty;

    public LogLevel LogLevel { get; set; } = LogLevel.Debug;
}
