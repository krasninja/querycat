using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QueryCat.Backend.Core.Utils;

public sealed partial class DynamicBuffer<T> where T : IEquatable<T>
{
    /// <summary>
    /// Helper class to read from <see cref="DynamicBuffer{T}" />.
    /// </summary>
    [DebuggerDisplay("Consumed = {Consumed}, Remaining = {Remaining}, Current = {Current}")]
    public sealed class DynamicBufferReader
    {
        private readonly DynamicBuffer<T> _buffer;
        private long _position;
        private readonly int _maxEndIndex;
        private BufferSegment? _segment;

        /// <summary>
        /// Current element.
        /// </summary>
        public T? Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _segment != null ? _segment.Buffer[_position % _buffer._chunkSize] : default;
        }

        /// <summary>
        /// Next element.
        /// </summary>
        public T? Next
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_segment == null || End)
                {
                    return default;
                }
                var offset = _position % _buffer._chunkSize;
                if (offset < _maxEndIndex)
                {
                    return _segment.Buffer[offset + 1];
                }
                return _segment.NextRef != null ? _segment.NextRef.Buffer[0] : default;
            }
        }

        /// <summary>
        /// Next element.
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
                var offset = _position % _buffer._chunkSize;
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
            get => _position + 1 >= _buffer._endPosition;
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
        /// Buffer length.
        /// </summary>
        public long Length => _buffer._size - Consumed;

        /// <summary>
        /// Current position.
        /// </summary>
        public DynamicBufferPosition Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_segment, (int)(_position % _buffer._chunkSize));
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
                return GetUnreadSpan(_segment, _position);
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
                throw new ArgumentOutOfRangeException(nameof(position), "Seek position is outside of the committed buffer range.");
            }
            _position = position.AbsolutePosition;
            _segment = (BufferSegment?)position.Segment;
        }

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

            // If we are in the current segment - use fast path.
            if (count < _maxEndIndex - _position % _buffer._chunkSize)
            {
                if (_position + count >= _buffer._endPosition)
                {
                    count = _buffer._endPosition - _position - 1;
                }
                _position += count;
                return count;
            }

            return AdvanceSlow(count);
        }

        private long AdvanceSlow(long count)
        {
            var initialPosition = _position;
            var target = Math.Min(_position + count, _buffer._endPosition - 1);

            while (_position < target)
            {
                var toAdvance = Math.Min(target - _position, GetRemainingInSegment());
                _position += toAdvance;

                if (_position >= target || !AdvanceToNextSegment())
                {
                    break;
                }
            }

            return _position - initialPosition;
        }

        /// <summary>
        /// Number of items between the current position and the last index of its segment.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetRemainingInSegment()
            => _maxEndIndex - (int)(_position % _buffer._chunkSize);

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

            // If we are in the current segment - use fast path.
            if (count <= _position % _buffer._chunkSize)
            {
                if (_position - count < _buffer._startPosition)
                {
                    count = _position - _buffer._startPosition;
                }
                _position -= count;
                return count;
            }

            return RewindSlow(count);
        }

        private long RewindSlow(long count)
        {
            var initialPosition = _position;
            var target = Math.Max(_position - count, _buffer._startPosition);

            while (_position > target)
            {
                var toRewind = Math.Min(_position - target, _position % _buffer._chunkSize);
                _position -= toRewind;

                if (_position <= target || !AdvanceToPreviousSegment())
                {
                    break;
                }
            }

            return initialPosition - _position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AdvanceToNextSegment()
        {
            if (_segment == null || _segment.NextRef == null)
            {
                return false;
            }
            _position += _buffer._chunkSize - _position % _buffer._chunkSize;
            _segment = _segment.NextRef;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AdvanceToPreviousSegment()
        {
            if (_segment == null || _segment.PrevRef == null)
            {
                return false;
            }

            _position -= _position % _buffer._chunkSize + 1;
            _segment = _segment.PrevRef;
            return true;
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

                int i;
                for (i = 0; i < remaining.Length && values.Contains(remaining[i]); i++)
                {
                    _position++;
                }

                // Still have the remain buffer - break.
                if (i < remaining.Length)
                {
                    break;
                }

                // Advanced to the end - get the next segment.
                _position--;
                if (!AdvanceToNextSegment())
                {
                    break;
                }
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

                int i;
                for (i = 0; i < remaining.Length && values.Contains(remaining[i]); i++)
                {
                    _position++;
                }

                // Still have the remain buffer - break.
                if (i < remaining.Length)
                {
                    break;
                }

                // Advanced to the end - get the next segment.
                _position--;
                if (!AdvanceToNextSegment())
                {
                    break;
                }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            if (End || _segment == null)
            {
                return false;
            }

            var segmentStartIndex = (int)(_position % _buffer._chunkSize);
            if (segmentStartIndex < _maxEndIndex)
            {
                if (!_segment.Buffer[segmentStartIndex + 1].Equals(next))
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

            var equal = nextSegment.Buffer[0].Equals(next);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<T> GetUnreadSpan(BufferSegment segment, long position)
        {
            var startIndex = (int)(position % _buffer._chunkSize);
            var length = (int)Math.Min(_buffer._endPosition - position, _buffer._chunkSize - startIndex);
            return new ReadOnlySpan<T>(segment.Buffer, startIndex, length);
        }
    }
}
