using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Execution thread with query statements preparation support.
/// </summary>
public interface IExecutionThreadPrepare : IExecutionThread
{
    /// <summary>
    /// Prepare delegate from query. The delegate is optimized object to execute the query multiple times.
    /// The method parses, analyzes and compiles the query into executable form.
    /// </summary>
    /// <param name="query">Query.</param>
    /// <returns>Prepared delegate.</returns>
    Func<CancellationToken, ValueTask<VariantValue>> Prepare(string query);
}
