using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Provides method to create input and output of specific format (type).
/// </summary>
public interface IRowsFormatter
{
    /// <summary>
    /// Create input from formatter.
    /// </summary>
    /// <param name="blob">Input stream BLOB.</param>
    /// <param name="key">Unique key to identify input.</param>
    /// <returns>Instance of <see cref="IRowsInput" />.</returns>
    IRowsInput OpenInput(IBlobData blob, string? key = null);

    /// <summary>
    /// Create output from formatter.
    /// </summary>
    /// <param name="blob">Output stream BLOB.</param>
    /// <returns>Instance of <see cref="IRowsOutput" />.</returns>
    IRowsOutput OpenOutput(IBlobData blob);
}
