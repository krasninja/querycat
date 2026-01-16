using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// ReSharper disable LocalizableElement

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// The class encapsulates the linked list of buffers. It keeps the ordered
/// list of buffers and can auto-grow if no enough space available. Also,
/// it keeps freed buffers for reuse. The class is not thread safe.
/// </summary>
/// <typeparam name="T">Buffer type.</typeparam>
[DebuggerDisplay("Size = {Size}, Buffers = {UsedBuffersCount}/{TotalBuffersCount}")]
public sealed partial class DynamicBuffer<T> where T : IEquatable<T>
{
    /*
     * Here is the typical internal representation of dynamic buffer.
     *
     * ooooooXXXX XXXXXXXXXX XXXXXXXXXX XXXXXXXNNN NNNNNooooo
     * ^     ^                          ^     ^        ^
     * 1     2                          3     4        5
     *
     * o - not available or advanced data
     * X - committed data
     * N - non-committed (allocated) data
     * 1 - bufferHead
     * 2 - startPosition = 7
     * 3 - bufferTail
     * 4 - endPosition = 37
     * 5 - allocatedPosition = 45
     *
     * Use case #1:
     * 1) dynamicBuffer.Write("12345"); // Write some data to the buffer. Buffer state is "12345".
     * 2) dynamicBuffer.Advance(3); // Advance cursor. Now buffer is "45".
     * 3) dynamicBuffer.Write("67"); // Write more data. Now buffer is "4567".
     * 4) dynamicBuffer.GetSpan(0); // Get buffer data.
     * 5) dynamicBuffer.Advance(20); // Move cursor to the end. Buffer is empty.
     *
     * Use case #2:
     * 1) var buf1 = dynamicBuffer.Allocate(); // Get available buffer.
     * 2) "123".CopyTo(buf1); // Copy "123" to buffer. The buffer is empty.
     * 3) dynamicBuffer.Commit(3); // Commit that we wrote only 3 characters.
     * 4) dynamicBuffer.GetSpan(0); // Get buffer data. It is "123".
     * 5) var buf2 = dynamicBuffer.Allocate(); // Get another empty buffer.
     * 6) "45".CopyTo(buf2); // Append 45. But dynamic buffer state is still "123".
     * 7) dynamicBuffer.Commit(2); // Dynamic buffer state "12345".
     */

    private readonly int _chunkSize;
    private readonly int _maxEndIndex;
    private readonly int _maxFreeBuffers;
    private long _currentSegmentStartIndex;

#if DEBUG
    // ReSharper disable once StaticMemberInGenericType
    private static int _segmentId;
#endif

    private long _size;

    /// <summary>
    /// Current buffer size.
    /// </summary>
    public long Size => _size;

    /// <summary>
    /// Whether the dynamic buffer contains any data.
    /// </summary>
    public bool IsEmpty => (ulong)_size == 0;

    /// <summary>
    /// Total buffers (used and free) count.
    /// </summary>
    public int TotalBuffersCount => _buffersList.Count + _freeBuffersList.Count;

    /// <summary>
    /// Used buffers count.
    /// </summary>
    public int UsedBuffersCount => _buffersList.Count;

    /// <summary>
    /// Chunk size.
    /// </summary>
    public int ChunkSize => _chunkSize;

    /// <summary>
    /// Start position.
    /// </summary>
    public DynamicBufferPosition Start
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_buffersList.Head, GetSegmentStartIndex(_buffersList.Head));
    }

    /// <summary>
    /// End position.
    /// </summary>
    public DynamicBufferPosition End
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_buffersList.Tail, GetSegmentEndIndex(_buffersList.Tail, _endPosition - 1));
    }

    private readonly BufferSegmentList _buffersList = new();
    private readonly BufferSegmentList _freeBuffersList = new();
    private long _allocatedPosition;
    private long _startPosition;
    private long _endPosition;
    private bool _allocatedFlag;
    private bool _requiresIndexesRebuild;

    /// <summary>
    /// Simple implementation of queue for <see cref="BufferSegment" />.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    private sealed class BufferSegmentList
    {
        public BufferSegment? Head { get; private set; }

        public BufferSegment? Tail { get; private set; }

        public int Count { get; private set; }

        public bool IsEmpty => Head == null;

        public bool IsAny => Head != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void AddFirst(BufferSegment segment)
        {
            if (Head == null)
            {
                segment.PrevRef = null;
                Head = segment;
                Tail = segment;
            }
            else
            {
                // 4   head -> 1 -> 2 -> 3 <- tail
                // head -> 4 -> 1 -> 2 -> 3 <- tail
                segment.NextRef = Head;
                segment.PrevRef = null;
                Head = segment;
            }
            Count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void AddLast(BufferSegment segment)
        {
            if (Tail == null)
            {
                segment.PrevRef = Tail;
                segment.NextRef = null;
                Head = segment;
                Tail = segment;
            }
            else
            {
                // 4   head -> 1 -> 2 -> 3 <- tail
                // head -> 1 -> 2 -> 3 -> 4 <- tail
                segment.PrevRef = Tail;
                Tail.NextRef = segment;
                Tail = segment;
                Tail.NextRef = null;
            }
            Count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public BufferSegment? PopFirst()
        {
            if (Head == null)
            {
                return null;
            }
            var head = Head;
            if (head.NextRef != null)
            {
                head.NextRef.PrevRef = null;
            }
            Head = Head.NextRef;
            Count--;
            if (Count == 0)
            {
                Tail = null;
            }
            head.PrevRef = null;
            return head;
        }

        public void Clear()
        {
            Head = null;
            Tail = null;
            Count = 0;
        }

        /// <summary>
        /// Validate the linked list state. For internal debug only.
        /// </summary>
        /// <returns>Returns <c>true</c> if the state is valid, <c>false</c> otherwise.</returns>
        private bool ValidateState()
        {
            if (Head == null && Tail != null)
            {
                return false;
            }
            if (Head != null && Tail == null)
            {
                return false;
            }

            var count = 0;
            BufferSegment? current = Head;
            while (current != null)
            {
                count++;
                current = current.NextRef;
            }
            if (count != Count)
            {
                return false;
            }

            return true;
        }
    }

    private sealed class BufferSegment : ReadOnlySequenceSegment<T>
    {
#if DEBUG
        private int SegmentId { get; } = _segmentId++;
#endif

        public static BufferSegment Empty { get; } = new([default!]);

        internal T[] Buffer { get; }

        private BufferSegment? _prevRef;

        internal BufferSegment? PrevRef
        {
            get => _prevRef;
            set => _prevRef = value;
        }

        private BufferSegment? _nextRef;

        internal BufferSegment? NextRef
        {
            get => _nextRef;
            set => Next = _nextRef = value;
        }

        internal long StartPosition { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetIndex(long index) => RunningIndex = index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<T> GetSpan(int startIndex, int endIndex)
            => new(Buffer, startIndex, endIndex - startIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferSegment(T[] buffer, long runningIndex = 0)
        {
            Buffer = buffer;
            Memory = new ReadOnlyMemory<T>(Buffer);
            RunningIndex = runningIndex;
        }

#if DEBUG
        /// <inheritdoc />
        public override string ToString() => $"Id = {SegmentId}, NextId = {_nextRef?.SegmentId}";
#endif
    }

    /// <summary>
    /// Represents position in the <see cref="DynamicBuffer{T}" />.
    /// </summary>
    public readonly struct DynamicBufferPosition : IEquatable<DynamicBufferPosition>
    {
        private readonly BufferSegment? _segment;

        /// <summary>
        /// Position segment.
        /// </summary>
        internal object? Segment => _segment;

        private readonly int _offset;

        /// <summary>
        /// Offset within segment buffer.
        /// </summary>
        internal int Offset => _offset;

        internal long AbsolutePosition => _segment != null ? _offset + _segment.StartPosition : _offset;

        public bool Empty => _segment == null;

        public T? Value => _segment != null ? _segment.Buffer[_offset] : default;

        public static bool operator ==(DynamicBufferPosition left, DynamicBufferPosition right) => left.Equals(right);

        public static bool operator !=(DynamicBufferPosition left, DynamicBufferPosition right) => !(left == right);

        public static DynamicBufferPosition Null => new(BufferSegment.Empty, 0);

        internal DynamicBufferPosition(object? segment, int offset)
        {
            _segment = (BufferSegment?)segment;
            _offset = offset;
        }

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is DynamicBufferPosition other && Equals(other);

        /// <inheritdoc />
        public bool Equals(DynamicBufferPosition other) => Equals(_segment, other._segment) && _offset == other._offset;

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(_segment, _offset);

        /// <inheritdoc />
        public override string ToString() => $"Offset = {Offset} ({AbsolutePosition}), Segment = {_segment}";
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="chunkSize">Chunk size for allocation.</param>
    /// <param name="maxFreeBuffers">Max number of free buffers to keep. Not defined
    /// by default.</param>
    public DynamicBuffer(int chunkSize = 4096, int maxFreeBuffers = -1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);
        _chunkSize = chunkSize;
        _maxEndIndex = _chunkSize - 1;
        _maxFreeBuffers = maxFreeBuffers;
    }

    /// <summary>
    /// Move the cursor by certain amount of elements.
    /// </summary>
    /// <param name="sizeToAdvance">Number of elements to move on.</param>
    public void Advance(long sizeToAdvance)
    {
        if (sizeToAdvance < 1)
        {
            return;
        }

        long advanced = 0;
        var currentSegment = _buffersList.Head;
        while (currentSegment != null && advanced < sizeToAdvance)
        {
            var chunkStartIndex = GetSegmentStartIndexLong();

            // For head buffer we should free remain space. For example, ooooooXXXX.
            if (chunkStartIndex > 0 && currentSegment == _buffersList.Head)
            {
                var remainSpaceInChunk = _chunkSize - chunkStartIndex;
                var remain = sizeToAdvance > remainSpaceInChunk ? remainSpaceInChunk : sizeToAdvance;
                remain = Math.Min(remain, Size);
                advanced += remain;
                _startPosition += remain;
            }
            // For tail buffer we can only free remain space.
            else if (currentSegment == _buffersList.Tail)
            {
                var remain = Min(sizeToAdvance - advanced, _chunkSize, Size);
                advanced += remain;
                _startPosition += remain;
            }
            else
            {
                var remain = sizeToAdvance - advanced > _chunkSize ? _chunkSize
                    : sizeToAdvance - advanced;
                remain = Math.Min(remain, Size);
                advanced += remain;
                _startPosition += remain;
            }
            SetSize();

            // Store freed segment, shrink main buffer.
            if (GetSegmentStartIndexLong() == 0)
            {
                var segment = _buffersList.PopFirst();
                // Update running indexes if _buffersList was updated.
                _requiresIndexesRebuild = true;
                if (segment != null)
                {
                    currentSegment = segment.NextRef;
                    if (_maxFreeBuffers == -1 || TotalBuffersCount < _freeBuffersList.Count)
                    {
                        _freeBuffersList.AddLast(segment);
                    }
                }
                else
                {
                    currentSegment = null;
                }
            }
            else
            {
                currentSegment = currentSegment.NextRef;
            }
        }
    }

    private void RebuildRunningIndexes()
    {
        if (!_requiresIndexesRebuild)
        {
            return;
        }

        var current = _buffersList.Head;
        var runningIndex = 0;
        while (current != null)
        {
            current.SetIndex(runningIndex);
            runningIndex += current.Memory.Length;
            current = current.NextRef;
        }
        _requiresIndexesRebuild = false;
    }

    /// <summary>
    /// Moves the cursor to the end of the sequence.
    /// </summary>
    public void AdvanceToEnd()
    {
        while (_buffersList.PopFirst() is { } segment)
        {
            _freeBuffersList.AddLast(segment);
        }
        _allocatedPosition = 0;
        _startPosition = 0;
        _endPosition = 0;
        _allocatedFlag = false;
        _size = 0;
    }

    #region Read

    /// <summary>
    /// Allocate the buffer of <see cref="ChunkSize" /> amount
    /// of elements.
    /// </summary>
    /// <returns>Buffer.</returns>
    public Memory<T> Allocate()
    {
        if (_allocatedFlag)
        {
            throw new InvalidOperationException("You should commit data before allocate a new buffer.");
        }
        _allocatedFlag = true;
        return AllocateInternal();
    }

    private Memory<T> AllocateInternal()
    {
        // Check if we have spare space at current chunk.
        if (_buffersList.IsAny && _endPosition % _chunkSize > 0)
        {
            var tailStartIndex = GetSegmentEndIndex();
            var bufferSize = _chunkSize - tailStartIndex;
            _allocatedPosition += bufferSize;
            return _buffersList.Tail!.Buffer.AsMemory(tailStartIndex, bufferSize);
        }

        return AddNextBufferSegment().Buffer;
    }

    private BufferSegment AddNextBufferSegment()
    {
        BufferSegment segment;

        // Before allocate check if we have available free segment.
        if (_freeBuffersList.IsAny)
        {
            segment = _freeBuffersList.PopFirst()!;
        }
        else
        {
            segment = new BufferSegment(GC.AllocateUninitializedArray<T>(_chunkSize));
        }

        _buffersList.AddLast(segment);
        segment.StartPosition = _currentSegmentStartIndex;
        _currentSegmentStartIndex += _chunkSize;
        _allocatedPosition += _chunkSize;
        _requiresIndexesRebuild = true;
        return segment;
    }

    /// <summary>
    /// Commit the buffer.
    /// </summary>
    /// <param name="buffer">Buffer to commit.</param>
    public void Commit(Span<T> buffer) => Commit(buffer.Length);

    /// <summary>
    /// Commit the buffer.
    /// </summary>
    /// <param name="buffer">Buffer to commit.</param>
    public void Commit(Memory<T> buffer) => Commit(buffer.Length);

    /// <summary>
    /// Commit the specific number of elements.
    /// </summary>
    /// <param name="size">Number of elements.</param>
    public void Commit(int size)
    {
        _allocatedFlag = false;
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        if (size == 0)
        {
            return;
        }

        _endPosition += size;
        SetSize();
        Debug.Assert(_endPosition <= _allocatedPosition,
            "Allocated position cannot be before committed!");
    }

    /// <summary>
    /// Get element at specific position.
    /// </summary>
    /// <param name="index">Element index.</param>
    /// <returns>Element.</returns>
    public T GetAt(int index)
    {
        var iterator = IteratorStart(index);
        if (iterator.IsNotEmpty)
        {
            return iterator.Segment.Buffer[iterator.StartIndex];
        }
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <summary>
    /// Try to get element by index.
    /// </summary>
    /// <param name="index">Element index.</param>
    /// <param name="value">Value or default.</param>
    /// <returns><c>True</c> if can get, <c>false</c> otherwise.</returns>
    public bool TryGetAt(int index, out T? value)
    {
        var iterator = IteratorStart(index);
        if (iterator.IsNotEmpty)
        {
            value = iterator.Segment.Buffer[iterator.StartIndex];
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Get data between start and end indexes.
    /// </summary>
    /// <param name="startIndex">Start index.</param>
    /// <param name="endIndex">End index.</param>
    /// <returns>Span.</returns>
    public ReadOnlySpan<T> Slice(int startIndex, int endIndex = -1)
    {
        var maxEndIndex = (int)Size;
        if (endIndex > maxEndIndex || endIndex == -1)
        {
            endIndex = maxEndIndex;
        }
        var spanSize = endIndex - startIndex;
        if (spanSize < 0 || startIndex < 0 || endIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Start index must be greater than end index.");
        }

        T[]? localBuffer = null;
        var localBufferStartIndex = 0;

        /*
         * oooXX XXXXX XXXXo
         * 01234 01234 01234
         * 0     5     10
         * #1    #2    #3
         */

        var iterator = IteratorStart(startIndex);
        while (iterator.IsNotEmpty)
        {
            var endIndexOffset = iterator.Consumed + iterator.Size;

            // Start.
            if (startIndex == iterator.Consumed)
            {
                // If start and end indexes are within buffer segment - return just as span.
                if (endIndex < endIndexOffset)
                {
                    return new ReadOnlySpan<T>(iterator.Segment.Buffer, iterator.StartIndex, spanSize);
                }
                localBuffer = new T[spanSize];
                Array.Copy(iterator.Segment.Buffer, iterator.StartIndex,
                    localBuffer, localBufferStartIndex, iterator.Size);
                localBufferStartIndex += iterator.Size;
            }
            // Middle.
            else if (endIndex > endIndexOffset)
            {
                Debug.Assert(localBuffer != null, "Null buffer!");
                Array.Copy(iterator.Segment.Buffer, iterator.StartIndex,
                    localBuffer, localBufferStartIndex, iterator.Size);
                localBufferStartIndex += iterator.Size;
            }
            // End.
            else
            {
                Debug.Assert(localBuffer != null, "Null buffer!");
                Array.Copy(iterator.Segment.Buffer, iterator.StartIndex,
                    localBuffer, localBufferStartIndex, spanSize - localBufferStartIndex);
                return localBuffer;
            }

            iterator = IteratorNext(iterator);
        }
        return localBuffer;
    }

    /// <summary>
    /// Get data between start and end positions.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="length">Length of the target span.</param>
    /// <returns>Span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> Slice(DynamicBufferPosition start, long length)
    {
        if (length < 1)
        {
            return [];
        }
        var startSegment = (BufferSegment?)start.Segment;
        if (startSegment == null)
        {
            return [];
        }

        // Fast case.
        if (length < _chunkSize - start.Offset)
        {
            return new ReadOnlySpan<T>(startSegment.Buffer, start.Offset, (int)length);
        }

        // Slow path.
        return SliceSlow(start, length);
    }

    private ReadOnlySpan<T> SliceSlow(DynamicBufferPosition start, long length)
    {
        var startSegment = (BufferSegment?)start.Segment;
        var targetPosition = start.AbsolutePosition + length - 1;
        var currentSegment = startSegment;
        var end = End;
        while (currentSegment != null)
        {
            if (targetPosition >= currentSegment.StartPosition && targetPosition <= currentSegment.StartPosition + _maxEndIndex)
            {
                end = new DynamicBufferPosition(currentSegment, (int)(targetPosition % _chunkSize));
                break;
            }
            currentSegment = currentSegment.NextRef;
        }

        return Slice(start, end);
    }

    /// <summary>
    /// Get data between start and end positions.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="end">End position.</param>
    /// <returns>Span.</returns>
    public ReadOnlySpan<T> Slice(DynamicBufferPosition start, DynamicBufferPosition end)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(true, start.Empty, nameof(start));

        var startSegment = (BufferSegment?)start.Segment;
        if (end.Empty)
        {
            end = End;
        }
        var endSegment = (BufferSegment?)end.Segment!;
        if (startSegment == null)
        {
            return [];
        }

        // Fast case.
        if (startSegment == endSegment)
        {
            return new ReadOnlySpan<T>(startSegment.Buffer, start.Offset, end.Offset - start.Offset + 1);
        }

        // Calculate target buffer size.
        var size = (int)(endSegment.StartPosition - startSegment.StartPosition + end.Offset - start.Offset + 1);

        // Fill buffer and return.
        var localBuffer = new T[size];
        var currentSegment = startSegment;
        var offset = 0;
        while (currentSegment != null && currentSegment != end.Segment)
        {
            if (currentSegment == start.Segment)
            {
                var length = _chunkSize - start.Offset;
                Array.Copy(currentSegment.Buffer, start.Offset, localBuffer, offset, length);
                offset += length;
            }
            else
            {
                Array.Copy(currentSegment.Buffer, 0, localBuffer, offset, _chunkSize);
                offset += _chunkSize;
            }
            currentSegment = currentSegment.NextRef;
        }
        if (currentSegment != null)
        {
            Array.Copy(currentSegment.Buffer, 0, localBuffer, offset, end.Offset + 1);
        }

        return localBuffer;
    }

    /// <summary>
    /// Get the first index of any specified delimiters.
    /// </summary>
    /// <param name="delimiters">The delimiters to look for.</param>
    /// <param name="foundDelimiter">Found delimiter.</param>
    /// <param name="skip">Start index to search from. Default is 0.</param>
    /// <returns>The delimiter index or -1 if not found.</returns>
    public int IndexOfAny(scoped ReadOnlySpan<T> delimiters, out T? foundDelimiter, int skip = 0)
    {
        var iterator = IteratorStart(skip);
        while (iterator.IsNotEmpty)
        {
            for (var i = iterator.StartIndex; i <= iterator.EndIndex; i++)
            {
                var foundDelimiterIndex = delimiters.IndexOf(iterator.Segment.Buffer[i]);
                if (foundDelimiterIndex > -1)
                {
                    foundDelimiter = delimiters[foundDelimiterIndex];
                    return iterator.Consumed + i - iterator.StartIndex;
                }
            }

            iterator = IteratorNext(iterator);
        }

        foundDelimiter = default;
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private long GetNextRunningIndex()
        => _buffersList.Tail != null ? _buffersList.Tail.RunningIndex + _buffersList.Tail.Memory.Length : 0;

    private readonly ref struct SegmentChunk
    {
        public BufferSegment Segment { get; }

        public int StartIndex { get; }

        public int EndIndex { get; }

        public int Size { get; }

        public int Consumed { get; }

        public bool IsEmpty => StartIndex == -1;

        public bool IsNotEmpty => StartIndex != -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SegmentChunk Empty() => new(BufferSegment.Empty, -1, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> GetSpan() => Segment.GetSpan(StartIndex, EndIndex);

        public SegmentChunk(BufferSegment segment, int startIndex, int endIndex, int consumed)
        {
            Segment = segment;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Consumed = consumed;
            Size = EndIndex - StartIndex + 1;
        }
    }

    #region Segment position

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentStartIndex(BufferSegment? bufferSegment)
        => bufferSegment == _buffersList.Head ? (int)(_startPosition % _chunkSize) : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentStartIndex(BufferSegment? bufferSegment, long position)
        => bufferSegment == _buffersList.Head ? (int)(position % _chunkSize) : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentStartIndex() => (int)(_startPosition % _chunkSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetSegmentStartIndexLong(BufferSegment? bufferSegment)
        => bufferSegment == _buffersList.Head ? _startPosition % _chunkSize : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetSegmentStartIndexLong() => _startPosition % _chunkSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentEndIndex(BufferSegment? bufferSegment)
        => bufferSegment == _buffersList.Tail && _endPosition % _chunkSize != 0
            ? (int)(_endPosition % _chunkSize)
            : _maxEndIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentEndIndex(BufferSegment? bufferSegment, long position)
        => bufferSegment == _buffersList.Tail && position % _chunkSize != 0
            ? (int)(position % _chunkSize)
            : _maxEndIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetSegmentEndIndexLong(BufferSegment? bufferSegment)
        => bufferSegment == _buffersList.Tail && _endPosition % _chunkSize != 0
            ? _endPosition % _chunkSize
            : _maxEndIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentEndIndex() => (int)(_endPosition % _chunkSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetSegmentEndIndexLong() => _endPosition % _chunkSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentLength(BufferSegment? bufferSegment)
    {
        var startIndex = GetSegmentStartIndex(bufferSegment);
        var endIndex = GetSegmentEndIndex(bufferSegment);
        return endIndex - startIndex;
    }

    #endregion

    private SegmentChunk IteratorStart(int startIndex = 0)
    {
        var bufferSegment = _buffersList.Head;
        if (bufferSegment == null)
        {
            return SegmentChunk.Empty();
        }

        var consumed = startIndex;
        var bufferStartIndex = GetSegmentStartIndex(bufferSegment);
        // Skip head segment.
        if (bufferStartIndex + startIndex >= _chunkSize)
        {
            startIndex -= _chunkSize - bufferStartIndex;
            bufferStartIndex = startIndex;
            bufferSegment = bufferSegment.NextRef;
        }
        else
        {
            bufferStartIndex += startIndex;
        }
        while (startIndex > _chunkSize && bufferSegment != null)
        {
            startIndex -= _chunkSize;
            bufferStartIndex = startIndex;
            bufferSegment = bufferSegment.NextRef;
        }
        if (bufferSegment == null)
        {
            return SegmentChunk.Empty();
        }

        var bufferEndIndex = GetSegmentEndIndex(bufferSegment);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bufferStartIndex, bufferEndIndex, nameof(startIndex));
        return new SegmentChunk(bufferSegment, bufferStartIndex, bufferEndIndex, consumed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SegmentChunk IteratorNext(scoped SegmentChunk segmentChunk)
    {
        if (segmentChunk.Segment.NextRef == null)
        {
            return SegmentChunk.Empty();
        }

        var nextSegment = segmentChunk.Segment.NextRef;
        var endIndex = GetSegmentEndIndex(nextSegment);
        return new SegmentChunk(nextSegment, 0, endIndex, segmentChunk.Consumed + segmentChunk.Size);
    }

    /// <summary>
    /// Get the total buffer without advanced data.
    /// </summary>
    /// <returns>The total sequence.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySequence<T> GetSequence()
    {
        if (_buffersList.Head == null || IsEmpty)
        {
            return ReadOnlySequence<T>.Empty;
        }
        RebuildRunningIndexes();
        var headStartIndex = GetSegmentStartIndex(_buffersList.Head);
        var tailEndIndex = _endPosition % _chunkSize == 0
            ? _chunkSize
            : GetSegmentEndIndex(_buffersList.Tail);
        return new ReadOnlySequence<T>(_buffersList.Head, headStartIndex, _buffersList.Tail!, tailEndIndex);
    }

    /// <summary>
    /// Get the total buffer without advanced data.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="length">Length of the sequence.</param>
    /// <returns>The total sequence.</returns>
    public ReadOnlySequence<T> GetSequence(DynamicBufferPosition start, long length)
    {
        if (_buffersList.Head == null || IsEmpty || length < 1)
        {
            return ReadOnlySequence<T>.Empty;
        }

        RebuildRunningIndexes();

        var targetStartPosition = start.AbsolutePosition;
        var targetEndPosition = start.AbsolutePosition + length;
        BufferSegment? startSegment = null;
        var startIndex = -1;

        var iterator = IteratorStart();
        while (iterator.IsNotEmpty)
        {
            if (targetStartPosition >= iterator.Segment.StartPosition && targetStartPosition < iterator.Segment.StartPosition + _chunkSize)
            {
                startSegment = iterator.Segment;
                startIndex = GetSegmentStartIndex(iterator.Segment, targetStartPosition);
            }
            if (targetEndPosition >= iterator.Segment.StartPosition && targetEndPosition < iterator.Segment.StartPosition + _chunkSize && startSegment != null)
            {
                var endSegment = iterator.Segment;
                var endIndex = (int)(targetEndPosition % _chunkSize);
                return new ReadOnlySequence<T>(startSegment, startIndex, endSegment, endIndex);
            }
            iterator = IteratorNext(iterator);
        }

        return GetSequence();
    }

    /// <summary>
    /// Get the total buffer without advanced data.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="end">End position.</param>
    /// <returns>The total sequence.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySequence<T> GetSequence(DynamicBufferPosition start, DynamicBufferPosition end)
    {
        var startSegment = (BufferSegment?)start.Segment;
        var startIndex = start.Offset % _chunkSize;
        var endSegment = (BufferSegment?)end.Segment;
        var endIndex = end.Offset % _chunkSize;
        if (startSegment == null || endSegment == null)
        {
            return ReadOnlySequence<T>.Empty;
        }
        RebuildRunningIndexes();
        return new ReadOnlySequence<T>(startSegment, startIndex, endSegment, ++endIndex);
    }

    /// <summary>
    /// Attempt to copy exact buffer size items.
    /// </summary>
    /// <param name="buffer">Output buffer.</param>
    /// <param name="advance">Should advance dynamic buffer.</param>
    /// <returns><c>True</c> if all data was read, <c>false</c> otherwise.</returns>
    public bool TryCopyExact(Span<T> buffer, bool advance = true)
    {
        var totalRead = 0;
        var bufferSize = buffer.Length;

        if (IsEmpty || bufferSize == 0)
        {
            return false;
        }

        var startIndex = GetSegmentStartIndex(_buffersList.Head);
        var endIndex = GetSegmentEndIndex(_buffersList.Head);

        // Fast path.
        if (_buffersList.Head != null && endIndex - startIndex >= bufferSize)
        {
            var span = _buffersList.Head.Buffer.AsSpan(startIndex, bufferSize);
            span.CopyTo(buffer);
            totalRead = span.Length;
        }
        // Slow path.
        else
        {
            var iterator = IteratorStart();
            while (iterator.IsNotEmpty)
            {
                var span = iterator.GetSpan();
                if (span.Length > buffer.Length)
                {
                    span = span.Slice(0, buffer.Length);
                }
                span.CopyTo(buffer);
                buffer = buffer.Slice(span.Length);
                totalRead += span.Length;
                iterator = IteratorNext(iterator);
            }
        }

        if (advance)
        {
            Advance(totalRead);
        }

        return totalRead == bufferSize;
    }

    /// <summary>
    /// Attempt to read exact buffer size items.
    /// </summary>
    /// <param name="count">Items to read.</param>
    /// <param name="buffer">Output buffer.</param>
    /// <param name="advance">Should advance dynamic buffer.</param>
    /// <returns><c>True</c> if all data was read, <c>false</c> otherwise.</returns>
    public bool TryReadExact(int count, out ReadOnlySpan<T> buffer, bool advance = true)
    {
        if (IsEmpty || count == 0)
        {
            buffer = ReadOnlySpan<T>.Empty;
            return false;
        }

        var startIndex = GetSegmentStartIndex(_buffersList.Head);
        var endIndex = GetSegmentEndIndex(_buffersList.Head);

        // Fast path.
        if (_buffersList.Head != null && endIndex - startIndex >= count)
        {
            buffer = _buffersList.Head.Buffer.AsSpan(startIndex, count);
        }
        // Slow path.
        else
        {
            var sequence = GetSequence();
            var newBuffer = GC.AllocateUninitializedArray<T>(sequence.Length > count ? count : (int)sequence.Length);
            sequence.Slice(0, count).CopyTo(newBuffer);
            buffer = newBuffer;
        }

        if (advance)
        {
            Advance(buffer.Length);
        }

        return buffer.Length == count;
    }

    #endregion

    #region Write

    /// <summary>
    /// Write value into buffer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="repeat">Number of times to repeat it.</param>
    public void Write(T value, int repeat = 1)
    {
        var arr = new T[repeat];
        Array.Fill(arr, value);
        Write(arr);
    }

    /// <summary>
    /// Write data into the buffer.
    /// </summary>
    /// <param name="data">Data to write.</param>
    public void Write(scoped ReadOnlySpan<T> data)
    {
        var writeIndex = 0;
        var length = data.Length;

        if (data.Length < 1)
        {
            return;
        }

        // Write values.
        while (writeIndex < length)
        {
            var buffer = _buffersList.Tail?.Buffer ?? AllocateInternal();
            var remainBuffer = (int)(_allocatedPosition - _endPosition);
            if (remainBuffer < 1)
            {
                buffer = AllocateInternal();
                remainBuffer = buffer.Length;
            }

            var position = GetSegmentEndIndex();
            var upperIndex = remainBuffer > data.Length - writeIndex ? data.Length : remainBuffer + writeIndex;
            data[writeIndex..upperIndex].CopyTo(buffer.Span[position..]);
            var append = upperIndex - writeIndex;
            _endPosition += append;
            writeIndex += append;
            SetSize();
        }
    }

    /// <summary>
    /// Write data with total right padding.
    /// </summary>
    /// <param name="data">Data to write.</param>
    /// <param name="totalWidth">Total dynamic buffer size.</param>
    /// <param name="paddingValue">The value to fill the remain space.</param>
    public void WritePadRight(scoped ReadOnlySpan<T> data, int totalWidth, T paddingValue)
    {
        Write(data);
        var paddingCount = totalWidth - data.Length;
        if (paddingCount > 0)
        {
            Write(paddingValue, paddingCount);
        }
    }

    /// <summary>
    /// Write data with total left padding.
    /// </summary>
    /// <param name="data">Data to write.</param>
    /// <param name="totalWidth">Total dynamic buffer size.</param>
    /// <param name="paddingValue">The value to fill the remain space.</param>
    public void WritePadLeft(scoped ReadOnlySpan<T> data, int totalWidth, T paddingValue)
    {
        var paddingCount = totalWidth - data.Length;
        if (paddingCount > 0)
        {
            Write(paddingValue, paddingCount);
        }
        Write(data);
    }

    #endregion

    /// <summary>
    /// Clear the buffer.
    /// </summary>
    public void Clear()
    {
        _buffersList.Clear();
        _freeBuffersList.Clear();
        _allocatedPosition = 0;
        _startPosition = 0;
        _currentSegmentStartIndex = 0;
        _endPosition = 0;
        _size = 0;
        _allocatedFlag = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetSize() => _size = _endPosition - _startPosition;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Min(ulong val1, ulong val2, ulong val3)
    {
        return Math.Min(val3, Math.Min(val1, val2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Min(long val1, long val2, long val3)
    {
        return Math.Min(val3, Math.Min(val1, val2));
    }
}
