namespace QueryCat.Backend.Core.Plugins;

[Serializable]
#pragma warning disable CA2229
public class PluginException : QueryCatException
#pragma warning restore CA2229
{
    public PluginException()
    {
    }

    public PluginException(string message) : base(message)
    {
    }
}
