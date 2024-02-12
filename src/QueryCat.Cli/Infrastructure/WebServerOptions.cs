using System.Net;

namespace QueryCat.Cli.Infrastructure;

internal sealed class WebServerOptions
{
    public string? Urls { get; set; }

    public string Password { get; set; } = string.Empty;

    public string FilesRoot { get; set; } = string.Empty;

    public IList<IPAddress> AllowedAddresses { get; set; } = Array.Empty<IPAddress>();

    public int? AllowedAddressesSlots { get; set; }
}
