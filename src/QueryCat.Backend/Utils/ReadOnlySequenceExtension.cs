using System.Buffers;

namespace QueryCat.Backend.Utils;

/// <summary>
/// Extensions for <see cref="ReadOnlySequence{T}" />.
/// </summary>
internal static class ReadOnlySequenceExtension
{
    public static T GetElementAt<T>(this ReadOnlySequence<T> sequence, long position)
        => sequence.Slice(position).FirstSpan[0];
}
