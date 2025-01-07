using System.Net;
using System.Text.Json;
using QueryCat.Backend.Core;

namespace QueryCat.Cli.Infrastructure;

internal partial class WebServer
{
    private async Task HandleInfoApiActionAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        var localPlugins = (await _executionThread.PluginsManager.ListAsync(localOnly: true, cancellationToken)).ToList();
        var dict = new WebServerReply
        {
            ["installedPlugins"] = localPlugins,
            ["version"] = Application.GetVersion(),
            ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim(),
            ["platform"] = Environment.Version,
            ["date"] = DateTimeOffset.Now,
        };
        await JsonSerializer.SerializeAsync(response.OutputStream, dict,
            SourceGenerationContext.Default.WebServerReply, cancellationToken);
    }
}
