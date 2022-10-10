using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The interface is to support rows writing to the output source.
/// </summary>
public interface IRowsOutput : IRowsSource
{
    /// <summary>
    /// Write row.
    /// </summary>
    /// <param name="row">Row to write.</param>
    void Write(Row row);
}
