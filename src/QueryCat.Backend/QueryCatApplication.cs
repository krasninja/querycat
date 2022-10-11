using QueryCat.Backend.Functions.StandardFunctions;

namespace QueryCat.Backend;

/// <summary>
/// Application information.
/// </summary>
public static class QueryCatApplication
{
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
