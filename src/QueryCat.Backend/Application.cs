using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QueryCat.Backend.Functions.StandardFunctions;

namespace QueryCat.Backend;

/// <summary>
/// Application information.
/// </summary>
public static class Application
{
    /// <summary>
    /// Default application log factory.
    /// </summary>
    public static ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

    /// <summary>
    /// Product name.
    /// </summary>
    public const string ProductName = "QueryCat";

    /// <summary>
    /// Full product name with version.
    /// </summary>
    /// <returns></returns>
    public static string GetProductFullName()
        => $"{ProductName} {InfoFunctions.GetVersion()}";
}
