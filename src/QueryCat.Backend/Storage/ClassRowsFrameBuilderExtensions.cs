using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;

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

    /// <summary>
    /// Build iterator based on <see cref="IEnumerable{T}" />.
    /// </summary>
    /// <param name="classRowsFrame">Instance of <see cref="ClassRowsFrameBuilder{TClass} " />.</param>
    /// <param name="enumerable">Enumerable.</param>
    /// <returns>Enumerable iterator instance.</returns>
    public static ClassRowsIterator<TClass> BuildIterator<TClass>(this ClassRowsFrameBuilder<TClass> classRowsFrame, IEnumerable<TClass> enumerable)
        where TClass : class
    {
        return new(
            classRowsFrame.Columns.ToArray(),
            classRowsFrame.Getters.ToArray(),
            enumerable);
    }
}
