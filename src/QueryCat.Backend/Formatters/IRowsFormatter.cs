using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Provides method to create input and output of specific format (type).
/// </summary>
public interface IRowsFormatter
{
    /// <summary>
    /// Create input formatter.
    /// </summary>
    /// <param name="input">Input stream.</param>
    /// <returns>Instance of <see cref="IRowsInput" />.</returns>
    IRowsInput OpenInput(Stream input);

    /// <summary>
    /// Create output formatter.
    /// </summary>
    /// <param name="output">Output stream.</param>
    /// <returns>Instance of <see cref="IRowsOutput" />.</returns>
    IRowsOutput OpenOutput(Stream output);
}
