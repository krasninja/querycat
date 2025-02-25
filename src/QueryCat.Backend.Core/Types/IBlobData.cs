namespace QueryCat.Backend.Core.Types;

/// <summary>
/// BLOB data provider.
/// </summary>
public interface IBlobData
{
    /// <summary>
    /// Length in bytes of the data.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// Return stream representation of BLOB data.
    /// </summary>
    /// <returns>Stream.</returns>
    Stream GetStream();
}
