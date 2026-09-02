using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace QueryCat.Backend.Core.Utils;

public sealed partial class DynamicBuffer<T> where T : IEquatable<T>
{
    /// <summary>
    /// Helper class to read from <see cref="DynamicBuffer{T}" />.
    /// </summary>
    /// <remarks>
    /// The reader position is always within the <c>[start position; end position]</c> range. The end
    /// position points right after the last committed element, so <see cref="Current" /> returns
    /// the default value and <see cref="End" /> returns <c>true</c> there.
    /// </remarks>
    [DebuggerDisplay("Consumed = {Consumed}, Remaining = {Remaining}, Current = {Current}")]
    public sealed class DynamicBufferReader
    {
        private readonly DynamicBuffer<T> _buffer;
        private long _position;
        private BufferSegment? _segment;

        /// <summary>
        /// Offset of the current position within the current segment. It equals to the chunk size
        /// if the position is right after the last element of the last segment.
        /// </summary>
        private int SegmentOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segment != null ? (int)(_position - _segment.AbsoluteStartPosition) : 0;
        }

        /// <summary>
        /// Current element.
        /// </summary>
        public T? Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var segment = EnsureSegment();
                if (segment == null || _position >= _buffer._endPosition || _position < _buffer._startPosition)
                {
                    return default;
                }
                return segment.Buffer[SegmentOffset];
            }
        }

        /// <summary>
        /// Next element.
        /// </summary>
        public T? Next
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var segment = EnsureSegment();
                if (segment == null || _position + 1 >= _buffer._endPosition)
                {
                    return default;
                }
                var offset = SegmentOffset;
                if (offset < _buffer._chunkSize - 1)
                {
                    return segment.Buffer[offset + 1];
                }
                return segment.NextRef != null ? segment.NextRef.Buffer[0] : default;
            }
        }

        /// <summary>
        /// Previous element.
        /// </summary>
        public T? Past
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var segment = EnsureSegment();
                if (segment == null || _position <= _buffer._startPosition || _position > _buffer._endPosition)
                {
                    return default;
                }
                var offset = SegmentOffset;
                if (offset > 0)
                {
                    return segment.Buffer[offset - 1];
                }
                return segment.PrevRef != null ? segment.PrevRef.Buffer[_buffer._chunkSize - 1] : default;
            }
        }

        /// <summary>
        /// Is at the end of the buffer.
        /// </summary>
        public bool End
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _position >= _buffer._endPosition;
        }

        /// <summary>
        /// Number of remaining items.
        /// </summary>
        public long Remaining => _buffer._endPosition - _position;

        /// <summary>
        /// Number of read items.
        /// </summary>
        public long Consumed => _position - _buffer._startPosition;

        /// <summary>
        /// Current position.
        /// </summary>
        public DynamicBufferPosition Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var segment = EnsureSegment();
                return segment != null
                    ? new DynamicBufferPosition(segment, (int)(_position - segment.AbsoluteStartPosition))
                    : DynamicBufferPosition.Null;
            }
        }

        /// <summary>
        /// Current unread span (buffer).
        /// </summary>
        public ReadOnlySpan<T> UnreadSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var segment = EnsureSegment();
                if (segment == null)
                {
                    return ReadOnlySpan<T>.Empty;
                }
                var startIndex = SegmentOffset;
                var length = (int)Math.Min(_buffer._endPosition - _position, _buffer._chunkSize - startIndex);
                return length > 0 ? new ReadOnlySpan<T>(segment.Buffer, startIndex, length) : ReadOnlySpan<T>.Empty;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="buffer">Instance of <see cref="DynamicBuffer{T}" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DynamicBufferReader(DynamicBuffer<T> buffer)
        {
            _buffer = buffer;
            _position = buffer._startPosition;
            _segment = buffer._buffersList.Head;
        }

        /// <summary>
        /// Reset reader state to the beginning of the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _position = _buffer._startPosition;
            _segment = _buffer._buffersList.Head;
        }

        /// <summary>
        /// Attach to the buffer head if the reader was created (or reset) while the buffer
        /// had no segments. Otherwise, the reader would stay empty forever even after
        /// the data has been written.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BufferSegment? EnsureSegment()
        {
            SyncSegment();
            if (_segment == null && _buffer._buffersList.Head != null)
            {
                _position = _buffer._startPosition;
                _segment = _buffer._buffersList.Head;
            }
            return _segment;
        }

        /// <summary>
        /// Make sure the current segment is the one that contains the current position. The position
        /// right after the last element of the last segment stays within that segment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SyncSegment()
        {
            if (_segment == null)
            {
                return;
            }
            while (_position - _segment.AbsoluteStartPosition >= _buffer._chunkSize && _segment.NextRef != null)
            {
                _segment = _segment.NextRef;
            }
            while (_position < _segment.AbsoluteStartPosition && _segment.PrevRef != null)
            {
                _segment = _segment.PrevRef;
            }
        }

        /// <summary>
        /// Seek the reader to the certain position of the buffer.
        /// </summary>
        /// <param name="position">Position to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(DynamicBufferPosition position)
        {
            var segment = (BufferSegment?)position.Segment;
            if (segment == null)
            {
                Reset();
                return;
            }
            var absolutePosition = position.AbsolutePosition;
            if (absolutePosition < _buffer._startPosition || absolutePosition > _buffer._endPosition)
            {
                ThrowSeekOutOfRange();
            }
            Debug.Assert(
                absolutePosition >= segment.AbsoluteStartPosition && absolutePosition <= segment.AbsoluteEndPosition,
                "The position segment either belongs to another buffer or has already been recycled.");
            _position = absolutePosition;
            _segment = segment;
            SyncSegment();
        }

        [DoesNotReturn]
        private static void ThrowSeekOutOfRange()
            => throw new ArgumentOutOfRangeException("position", "Seek position is outside of the committed buffer range.");

        /// <summary>
        /// Move the reader ahead the specified number of items.
        /// </summary>
        /// <param name="count">Number of items to advance.</param>
        /// <returns>Number of advanced items.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Advance(long count)
        {
            if (count < 1 || EnsureSegment() == null)
            {
                return 0;
            }

            var target = Math.Min(_position + count, _buffer._endPosition);
            if (target <= _position)
            {
                return 0;
            }

            // If we stay within the current segment - use fast path.
            if (_segment != null && _segment.Contains(target))
            {
                var advanced = target - _position;
                _position = target;
                return advanced;
            }

            return AdvanceSlow(target);
        }

        private long AdvanceSlow(long target)
        {
            var initialPosition = _position;

            while (_position < target)
            {
                _position = Math.Min(target, _segment!.AbsoluteStartPosition + _buffer._chunkSize);
                if (_position >= target || _segment.NextRef == null)
                {
                    break;
                }
                _segment = _segment.NextRef;
            }

            SyncSegment();
            return _position - initialPosition;
        }

        /// <summary>
        /// Move the reader back the specified number of items.
        /// </summary>
        /// <param name="count">Number of items to rewind.</param>
        /// <returns>Number of rewind items.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Rewind(long count)
        {
            if (count < 1 || EnsureSegment() == null)
            {
                return 0;
            }

            var target = Math.Max(_position - count, _buffer._startPosition);
            if (target >= _position)
            {
                return 0;
            }

            // If we stay within the current segment - use fast path.
            if (target >= _segment!.AbsoluteStartPosition)
            {
                var rewound = _position - target;
                _position = target;
                return rewound;
            }

            return RewindSlow(target);
        }

        private long RewindSlow(long target)
        {
            var initialPosition = _position;

            while (_position > target)
            {
                _position = Math.Max(target, _segment!.AbsoluteStartPosition);
                if (_position <= target || _segment.PrevRef == null)
                {
                    break;
                }
                _segment = _segment.PrevRef;
            }

            SyncSegment();
            return initialPosition - _position;
        }

        /// <summary>
        /// Skip consecutive instances any of the given <paramref name="values" />.
        /// </summary>
        /// <returns>How many positions the reader has been advanced.</returns>
        public long AdvancePastAny(scoped ReadOnlySpan<T> values)
        {
            if (EnsureSegment() == null)
            {
                return 0;
            }
            var initialPosition = _position;

            while (true)
            {
                var remaining = UnreadSpan;
                if (remaining.IsEmpty)
                {
                    break;
                }

                var index = remaining.IndexOfAnyExcept(values);
                if (index > -1)
                {
                    _position += index;
                    break;
                }

                // The whole span matches - move on to the next segment.
                _position += remaining.Length;
                SyncSegment();
            }

            return _position - initialPosition;
        }

        /// <summary>
        /// Skip consecutive instances any of the given <paramref name="values" />.
        /// </summary>
        /// <returns>How many positions the reader has been advanced.</returns>
        public long AdvancePastAny(SearchValues<T> values)
        {
            if (EnsureSegment() == null)
            {
                return 0;
            }
            var initialPosition = _position;

            while (true)
            {
                var remaining = UnreadSpan;
                if (remaining.IsEmpty)
                {
                    break;
                }

                var index = remaining.IndexOfAnyExcept(values);
                if (index > -1)
                {
                    _position += index;
                    break;
                }

                // The whole span matches - move on to the next segment.
                _position += remaining.Length;
                SyncSegment();
            }

            return _position - initialPosition;
        }

        /// <summary>
        /// Searches for any of a number of specified delimiters and optionally advances past the first one to be found.
        /// </summary>
        /// <param name="delimiters">The delimiters to search for.</param>
        /// <param name="advancePastDelimiter">True to move past the first found instance any of the given <paramref name="delimiters" />.</param>
        /// <returns>True if any of the given <paramref name="delimiters" /> were found.</returns>
        public bool TryAdvanceToAny(scoped ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
        {
            if (EnsureSegment() == null)
            {
                return false;
            }

            foreach (var chunk in new ChunkIterator(_buffer, Position))
            {
                var index = chunk.Span.IndexOfAny(delimiters);
                if (index > -1)
                {
                    SeekToDelimiter(chunk, index, advancePastDelimiter);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Searches for any of a number of specified delimiters and optionally advances past the first one to be found.
        /// </summary>
        /// <param name="delimiters">The delimiters to search for.</param>
        /// <param name="advancePastDelimiter">True to move past the first found instance any of the given <paramref name="delimiters" />.</param>
        /// <returns>True if any of the given <paramref name="delimiters" /> were found.</returns>
        public bool TryAdvanceToAny(SearchValues<T> delimiters, bool advancePastDelimiter = true)
        {
            if (EnsureSegment() == null)
            {
                return false;
            }

            foreach (var chunk in new ChunkIterator(_buffer, Position))
            {
                var index = chunk.Span.IndexOfAny(delimiters);
                if (index > -1)
                {
                    SeekToDelimiter(chunk, index, advancePastDelimiter);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Advance until the given <paramref name="delimiter" />, if found.
        /// </summary>
        /// <param name="delimiter">The delimiter to search for.</param>
        /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter" /> if found.</param>
        /// <returns>True if the given <paramref name="delimiter" /> was found.</returns>
        public bool TryAdvanceTo(T delimiter, bool advancePastDelimiter = true)
        {
            if (EnsureSegment() == null)
            {
                return false;
            }

            foreach (var chunk in new ChunkIterator(_buffer, Position))
            {
                var index = chunk.Span.IndexOf(delimiter);
                if (index > -1)
                {
                    SeekToDelimiter(chunk, index, advancePastDelimiter);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Move the reader onto the delimiter found within the chunk and optionally step over it.
        /// </summary>
        /// <param name="chunk">The chunk the delimiter was found in.</param>
        /// <param name="index">The delimiter index within the chunk span.</param>
        /// <param name="advancePastDelimiter">True to move past the delimiter.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SeekToDelimiter(SegmentChunk chunk, int index, bool advancePastDelimiter)
        {
            _segment = chunk.Segment;
            _position = chunk.Segment.AbsoluteStartPosition + chunk.StartIndex + index;
            SyncSegment();
            if (advancePastDelimiter)
            {
                Advance(1);
            }
        }

        /// <summary>
        /// Moves the reader to the end of the dynamic buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdvanceToEnd()
        {
            var endSegment = (BufferSegment?)_buffer.End.Segment;
            if (endSegment == null)
            {
                Reset();
                return;
            }
            _segment = endSegment;
            _position = _buffer._endPosition;
        }

        /// <summary>
        /// Check to see if the given <paramref name="next" /> value is next.
        /// </summary>
        /// <param name="next">The value to compare the next items to.</param>
        /// <param name="advancePast">Move past the <paramref name="next" /> value if found.</param>
        public bool IsNext(T? next, bool advancePast = false)
        {
            var segment = EnsureSegment();
            if (segment == null || _position + 1 >= _buffer._endPosition)
            {
                return false;
            }

            var segmentStartIndex = SegmentOffset;
            if (segmentStartIndex < _buffer._chunkSize - 1)
            {
                if (!EqualityComparer<T>.Default.Equals(segment.Buffer[segmentStartIndex + 1], next))
                {
                    return false;
                }
                if (advancePast)
                {
                    _position++;
                }
                return true;
            }

            var nextSegment = segment.NextRef;
            if (nextSegment == null)
            {
                return false;
            }

            var equal = EqualityComparer<T>.Default.Equals(nextSegment.Buffer[0], next);
            if (equal && advancePast)
            {
                _position++;
                SyncSegment();
            }
            return equal;
        }

        /// <summary>
        /// Get the position according to the given offset.
        /// </summary>
        /// <param name="offset">Position offset.</param>
        /// <returns>Instance of <see cref="DynamicBufferPosition" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DynamicBufferPosition GetPosition(long offset) => _buffer.GetPosition(offset, Position);
    }
}
