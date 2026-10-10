namespace QueryCat.Backend.Core.Plugins;

/// <summary>
/// QueryCat plugin exception.
/// </summary>
public class PluginException : QueryCatException
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
