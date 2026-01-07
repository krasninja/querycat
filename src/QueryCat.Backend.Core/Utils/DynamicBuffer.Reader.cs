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
        private BufferSegment? _segment;

        /// <summary>
        /// Current element.
        /// </summary>
        public T? Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _segment != null ? _segment.Buffer[_position % _buffer._chunkSize] : default; }
        }

        /// <summary>
        /// Next element.
        /// </summary>
        public T? Next
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_segment == null)
                {
                    return default;
                }
                var offset = _position % _buffer._chunkSize;
                if (offset < _buffer._chunkSize)
                {
                    return _segment.Buffer[offset];
                }
                if (_segment.NextRef == null)
                {
                    return default;
                }
                return _segment.NextRef.Buffer[0];
            }
        }

        /// <summary>
        /// Is at the end of the buffer.
        /// </summary>
        public bool End => _position >= _buffer._endPosition;

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
        public DynamicBufferPosition Position => new(_segment, (int)(_position % _buffer._chunkSize));

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
            _segment = _buffer._buffersList.Head;
        }

        /// <summary>
        /// Reset reader state to the beginning of the buffer.
        /// </summary>
        public void Reset()
        {
            _position = _buffer._startPosition;
            _segment = _buffer._buffersList.Head;
        }

        /// <summary>
        /// Move the reader ahead the specified number of items.
        /// </summary>
        /// <param name="count">Number of items to advance.</param>
        /// <returns>Number of advanced items.</returns>
        public long Advance(long count)
        {
            if (count < 1 || End || _segment == null)
            {
                return 0;
            }
            long advanced = 0;

            while (true)
            {
                var maxInCurrentSegment = _buffer.GetSegmentLength(_segment);
                var toAdvance = Math.Min(count - advanced, maxInCurrentSegment - 1);
                _position += toAdvance;
                advanced += toAdvance;

                if (advanced >= count)
                {
                    break;
                }

                if (!AdvanceToNextSegment())
                {
                    _position--;
                    break;
                }
                advanced++;
            }

            return advanced;
        }

        /// <summary>
        /// Move the reader back the specified number of items.
        /// </summary>
        /// <param name="count">Number of items to rewind.</param>
        /// <returns>Number of rewind items.</returns>
        public long Rewind(long count)
        {
            long rewind = 0;

            while (true)
            {
                var maxInCurrentSegment = _buffer.GetSegmentLength(_segment);
                var toRewind = Math.Min(count - rewind, maxInCurrentSegment - 1);
                _position -= toRewind;
                rewind += toRewind;

                if (rewind >= count)
                {
                    break;
                }

                if (!AdvanceToPreviousSegment())
                {
                    _position++;
                    break;
                }
                rewind++;
            }

            return rewind;
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
            if (_segment == null)
            {
                return false;
            }

            var previousSegment = _segment.PrevRef;
            _position -= _position % _buffer._chunkSize + 1;
            _segment = previousSegment;
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

                int i;
                for (i = 0; i < remaining.Length && values.IndexOf(remaining[i]) != -1; i++)
                {
                    _position++;
                }

                // Still have the remain buffer - break.
                if (i < remaining.Length - 1)
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

            var segment = _segment;
            var newPosition = _position;
            while (segment != null)
            {
                var remaining = GetUnreadSpan(segment, newPosition);
                var index = remaining.IndexOfAny(delimiters);
                if (index > -1)
                {
                    _position = newPosition;
                    _segment = segment;
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

                newPosition += remaining.Length;
                segment = segment.NextRef;
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

            var segment = _segment;
            var newPosition = _position;
            while (segment != null)
            {
                var remaining = GetUnreadSpan(segment, newPosition);
                var index = remaining.IndexOf(delimiter);
                if (index > -1)
                {
                    _position = newPosition;
                    _segment = segment;
                    if (!advancePastDelimiter)
                    {
                        _segment = segment;
                        _position += index;
                    }
                    else
                    {
                        Advance(index + 1);
                    }
                    return true;
                }

                newPosition += remaining.Length;
                segment = segment.NextRef;
            }

            return false;
        }

        /// <summary>
        /// Check to see if the given <paramref name="next" /> value is next.
        /// </summary>
        /// <param name="next">The value to compare the next items to.</param>
        /// <param name="advancePast">Move past the <paramref name="next" /> value if found.</param>
        public bool IsNext(T next, bool advancePast = false)
        {
            if (End || _segment == null)
            {
                return false;
            }

            var segmentStartIndex = _buffer.GetSegmentStartIndex(_segment, _position);
            if (segmentStartIndex < _buffer._chunkSize - 1)
            {
                if (_segment.Buffer[segmentStartIndex + 1].Equals(next))
                {
                    if (advancePast)
                    {
                        _position++;
                    }
                    return true;
                }
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
        public DynamicBufferPosition GetPosition(long offset)
        {
            if (_segment == null)
            {
                return DynamicBufferPosition.Null;
            }

            if (offset == 0)
            {
                return Position;
            }

            var current = _segment;
            var targetPosition = _position + offset;

            if (targetPosition <= _buffer._startPosition)
            {
                return _buffer.Start;
            }
            if (targetPosition >= _buffer._endPosition)
            {
                return _buffer.End;
            }

            var abs = _position - _position % _buffer._chunkSize;
            var currentStartIndex = _buffer.GetSegmentStartIndex(current) + abs;
            var currentEndIndex = _buffer.GetSegmentEndIndex(current) + abs - 1;
            if (offset > 0)
            {
                while (current != null)
                {
                    if (targetPosition >= currentStartIndex && targetPosition <= currentEndIndex)
                    {
                        return new DynamicBufferPosition(current, (int)targetPosition % _buffer._chunkSize);
                    }
                    currentStartIndex += _buffer._chunkSize;
                    currentEndIndex += _buffer._chunkSize;
                    current = current.NextRef;
                }
            }
            else
            {
                while (current != null)
                {
                    if (targetPosition >= currentStartIndex && targetPosition <= currentEndIndex)
                    {
                        return new DynamicBufferPosition(current, (int)targetPosition % _buffer._chunkSize);
                    }
                    currentStartIndex -= _buffer._chunkSize;
                    currentEndIndex -= _buffer._chunkSize;
                    current = current.PrevRef;
                }
            }

            return DynamicBufferPosition.Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<T> GetUnreadSpan(BufferSegment segment, long position)
        {
            var startIndex = (int)(position % _buffer._chunkSize);
            var endIndex = Math.Min(_buffer._endPosition - position, _buffer._chunkSize - startIndex);
            return new ReadOnlySpan<T>(segment.Buffer, startIndex, (int)endIndex);
        }
    }
}
