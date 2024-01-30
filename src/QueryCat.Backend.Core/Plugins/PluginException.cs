namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// QueryCat plugin exception.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class PluginException : QueryCatException
#pragma warning restore CA2229
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public PluginException()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Error message.</param>
    public PluginException(string message) : base(message)
    {
    }
}
