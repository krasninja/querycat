namespace QueryCat.Backend.Abstractions;

/// <summary>
/// Provides method to create input and output of specific format (type).
/// </summary>
public interface IRowsFormatter
{
    /// <summary>
    /// Create input formatter.
    /// </summary>
    /// <param name="input">Input stream.</param>
    /// <param name="key">Unique key.</param>
    /// <returns>Instance of <see cref="IRowsInput" />.</returns>
    IRowsInput OpenInput(Stream input, string? key = null);

    /// <summary>
    /// Create output formatter.
    /// </summary>
    /// <param name="output">Output stream.</param>
    /// <returns>Instance of <see cref="IRowsOutput" />.</returns>
    IRowsOutput OpenOutput(Stream output);
}
