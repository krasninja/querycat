using QueryCat.Backend.Types;

namespace QueryCat.Backend.Abstractions;

/// <summary>
/// The execution thread allows to run string commands.
/// </summary>
public interface IExecutionThread
{
    /// <summary>
    /// Run text query.
    /// </summary>
    /// <param name="query">Query.</param>
    VariantValue Run(string query);
}
