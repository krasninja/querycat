using QueryCat.Backend.Types;

namespace QueryCat.Backend.Abstractions;

/// <summary>
/// The execution thread allows to run string commands.
/// </summary>
public interface IExecutionThread : IDisposable
{
    /// <summary>
    /// The token source to force current query cancel.
    /// </summary>
    CancellationTokenSource CancellationTokenSource { get; }

    /// <summary>
    /// Run text query.
    /// </summary>
    /// <param name="query">Query.</param>
    VariantValue Run(string query);
}
