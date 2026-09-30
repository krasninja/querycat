namespace QueryCat.Backend.Core.Types;

/// <summary>
/// BLOB data provider.
/// </summary>
public interface IBlobData
{
    /// <summary>
    /// Logical BLOB name, optional.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Length in bytes of the data. Returns -1 if length is not available.
    /// </summary>
    long Length { get; }

    /// <summary>
    /// BLOB content MIME-type.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Return stream representation of BLOB data.
    /// </summary>
    /// <returns>Stream.</returns>
    Stream GetStream();
}
