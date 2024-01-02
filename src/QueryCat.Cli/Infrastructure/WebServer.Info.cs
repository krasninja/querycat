using System.Net;
using System.Text.Json;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Cli.Infrastructure;

internal partial class WebServer
{
    private void HandleInfoApiAction(HttpListenerRequest request, HttpListenerResponse response)
    {
        var localPlugins = AsyncUtils.RunSync(async ct
            => await _executionThread.PluginsManager.ListAsync(localOnly: true, ct))!.ToList();
        var dict = new WebServerReply
        {
            ["installedPlugins"] = localPlugins,
            ["version"] = Application.GetVersion(),
            ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim(),
            ["platform"] = Environment.Version,
            ["date"] = DateTimeOffset.Now,
        };
        JsonSerializer.Serialize(response.OutputStream, dict, SourceGenerationContext.Default.WebServerReply);
    }
}
