using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Relational;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Extensions for <see cref="ClassRowsFrameBuilder{TClass} " />.
/// </summary>
public static class ClassRowsFrameBuilderExtensions
{
    /// <summary>
    /// Build instance of <see cref="RowsFrame" />.
    /// </summary>
    /// <param name="classRowsFrame">Instance of <see cref="ClassRowsFrameBuilder{TClass} " />.</param>
    /// <param name="options">Options.</param>
    /// <returns>Instance of <see cref="ClassRowsFrame{TClass}" />.</returns>
    public static ClassRowsFrame<TClass> BuildRowsFrame<TClass>(this ClassRowsFrameBuilder<TClass> classRowsFrame, RowsFrameOptions? options = null)
        where TClass : class
    {
        return new(
            options ?? new RowsFrameOptions(),
            classRowsFrame.Columns.ToArray(),
            classRowsFrame.Getters.ToArray());
    }
}
