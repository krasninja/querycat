namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Try to resolve URI into function call. It is needed to resolve
/// "https://ya.ru" to Curl call and "C:\1.txt" into ReadFile call.
/// The execution context contains the chain to test.
/// </summary>
public interface IUriResolver
{
    /// <summary>
    /// Try to resolve URI to a real function call.
    /// </summary>
    /// <param name="uri">URI.</param>
    /// <param name="functionName">Resolved function name or null.</param>
    /// <returns><c>True</c> if resolved, <c>false</c> otherwise.</returns>
    bool TryResolve(string uri, out string? functionName);
}
