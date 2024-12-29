using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// The interface is to support rows writing to the output source.
/// </summary>
public interface IRowsOutput : IRowsSource
{
    /// <summary>
    /// Options.
    /// </summary>
    RowsOutputOptions Options { get; }

    /// <summary>
    /// Write row.
    /// </summary>
    /// <param name="values">Values to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<ErrorCode> WriteValuesAsync(VariantValue[] values, CancellationToken cancellationToken = default);
}
