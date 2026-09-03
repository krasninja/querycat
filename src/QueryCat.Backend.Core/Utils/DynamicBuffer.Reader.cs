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
        private readonly int _maxEndIndex;
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
                if (_segment == null || _position >= _buffer._endPosition || _position < _buffer._startPosition)
                {
                    return default;
                }
                return _segment.Buffer[SegmentOffset];
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
                if (_segment == null || _position + 1 >= _buffer._endPosition)
                {
                    return default;
                }
                var offset = SegmentOffset;
                if (offset < _maxEndIndex)
                {
                    return _segment.Buffer[offset + 1];
                }
                return _segment.NextRef != null ? _segment.NextRef.Buffer[0] : default;
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
                if (_segment == null || _position <= _buffer._startPosition)
                {
                    return default;
                }
                var offset = SegmentOffset;
                if (offset > 0)
                {
                    return _segment.Buffer[offset - 1];
                }
                return _segment.PrevRef != null ? _segment.PrevRef.Buffer[_maxEndIndex] : default;
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
                return _segment != null
                    ? new DynamicBufferPosition(_segment, (int)(_position - _segment.AbsoluteStartPosition))
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
                if (_segment == null)
                {
                    return ReadOnlySpan<T>.Empty;
                }
                var startIndex = SegmentOffset;
                var length = (int)Math.Min(_buffer._endPosition - _position, _buffer._chunkSize - startIndex);
                return length > 0 ? new ReadOnlySpan<T>(_segment.Buffer, startIndex, length) : ReadOnlySpan<T>.Empty;
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
            _position = _buffer._startPosition;
            _segment = !_buffer.IsEmpty ? _buffer._buffersList.Head : null;
            _maxEndIndex = _buffer._chunkSize - 1;
        }

        /// <summary>
        /// Reset reader state to the beginning of the buffer.
        /// </summary>
        public void Reset()
        {
            _position = _buffer._startPosition;
            _segment = !_buffer.IsEmpty ? _buffer._buffersList.Head : null;
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
                ThrowSeekOutOfRange(nameof(position));
            }
            _position = position.AbsolutePosition;
            _segment = (BufferSegment?)position.Segment;
            FixBoundPositionsCase();
        }

        [DoesNotReturn]
        private static void ThrowSeekOutOfRange(string paramName)
            // ReSharper disable once LocalizableElement
            => throw new ArgumentOutOfRangeException(paramName, "Seek position is outside of the committed buffer range.");

        /// <summary>
        /// Move the reader ahead the specified number of items.
        /// </summary>
        /// <param name="count">Number of items to advance.</param>
        /// <returns>Number of advanced items.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Advance(long count)
        {
            if (count < 1 || _segment == null)
            {
                return 0;
            }

            var target = count >= _buffer._endPosition - _position ? _buffer._endPosition : _position + count;
            if (target <= _position)
            {
                return 0;
            }

            // If we stay within the current segment - use fast path.
            if (target - _segment.AbsoluteStartPosition < _buffer._chunkSize)
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

            FixBoundPositionsCase();
            return _position - initialPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FixBoundPositionsCase()
        {
            if (_segment != null && _position >= _segment.AbsoluteEndPosition && _segment.NextRef != null)
            {
                _segment = _segment.NextRef;
            }
        }

        /// <summary>
        /// Move the reader back the specified number of items.
        /// </summary>
        /// <param name="count">Number of items to rewind.</param>
        /// <returns>Number of rewind items.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Rewind(long count)
        {
            if (count < 1 || _segment == null)
            {
                return 0;
            }

            var target = Math.Max(_position - count, _buffer._startPosition);
            if (target >= _position)
            {
                return 0;
            }

            // If we are in the current segment - use fast path.
            if (target >= _segment.AbsoluteStartPosition)
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

            FixBoundPositionsCase();
            return initialPosition - _position;
        }

        /// <summary>
        /// Skip consecutive instances any of the given <paramref name="values" />.
        /// </summary>
        /// <returns>How many positions the reader has been advanced.</returns>
        public long AdvancePastAny(scoped ReadOnlySpan<T> values)
        {
            if (_segment == null)
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

                _position += remaining.Length;
                FixBoundPositionsCase();
            }

            return _position - initialPosition;
        }

        /// <summary>
        /// Skip consecutive instances any of the given <paramref name="values" />.
        /// </summary>
        /// <returns>How many positions the reader has been advanced.</returns>
        public long AdvancePastAny(SearchValues<T> values)
        {
            if (_segment == null)
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

                _position += remaining.Length;
                FixBoundPositionsCase();
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
            if (_segment == null)
            {
                return false;
            }

            foreach (var chunk in new ChunkIterator(_buffer, Position))
            {
                var index = chunk.Span.IndexOfAny(delimiters);
                if (index > -1)
                {
                    _position = chunk.StartIndex + chunk.Segment.AbsoluteStartPosition;
                    _segment = chunk.Segment;
                    if (!advancePastDelimiter)
                    {
                        _position += index;
                    }
                    else
                    {
                        Advance(index + 1);
                    }
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
            if (_segment == null)
            {
                return false;
            }

            foreach (var chunk in new ChunkIterator(_buffer, Position))
            {
                var index = chunk.Span.IndexOfAny(delimiters);
                if (index > -1)
                {
                    _position = chunk.StartIndex + chunk.Segment.AbsoluteStartPosition;
                    _segment = chunk.Segment;
                    if (!advancePastDelimiter)
                    {
                        _position += index;
                    }
                    else
                    {
                        Advance(index + 1);
                    }
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
            if (_segment == null)
            {
                return false;
            }

            foreach (var chunk in new ChunkIterator(_buffer, Position))
            {
                var remaining = chunk.Span;
                var index = remaining.IndexOf(delimiter);
                if (index > -1)
                {
                    _position = chunk.StartIndex + chunk.Segment.AbsoluteStartPosition;
                    _segment = chunk.Segment;
                    if (!advancePastDelimiter)
                    {
                        _position += index;
                    }
                    else
                    {
                        Advance(index + 1);
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Moves the reader to the end of the dynamic buffer.
        /// </summary>
        public void AdvanceToEnd()
        {
            _segment = _buffer._buffersList.Tail;
            _position = _buffer._endPosition;
        }

        /// <summary>
        /// Check to see if the given <paramref name="next" /> value is next.
        /// </summary>
        /// <param name="next">The value to compare the next items to.</param>
        /// <param name="advancePast">Move past the <paramref name="next" /> value if found.</param>
        public bool IsNext(T? next, bool advancePast = false)
        {
            if (_segment == null || _position + 1 >= _buffer._endPosition)
            {
                return false;
            }

            var segmentStartIndex = SegmentOffset;
            if (segmentStartIndex < _maxEndIndex)
            {
                if (!EqualityComparer<T>.Default.Equals(_segment.Buffer[segmentStartIndex + 1], next))
                {
                    return false;
                }
                if (advancePast)
                {
                    _position++;
                }
                return true;
            }

            var nextSegment = _segment.NextRef;
            if (nextSegment == null)
            {
                return false;
            }

            var equal = EqualityComparer<T>.Default.Equals(nextSegment.Buffer[0], next);
            if (equal && advancePast)
            {
                _position++;
                _segment = nextSegment;
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
