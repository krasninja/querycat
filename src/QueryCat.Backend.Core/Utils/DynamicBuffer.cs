using System.Buffers;
using System.Collections;
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
    public bool IsEmpty => _size == 0;

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
        get
        {
            if (_endPosition <= 0 || _buffersList.Tail == null)
            {
                return Start;
            }
            return new DynamicBufferPosition(_buffersList.Tail, (int)(_endPosition - _buffersList.Tail.AbsoluteStartPosition));
        }
    }

    private readonly BufferSegmentList _buffersList = new();
    private readonly BufferSegmentList _freeBuffersList = new();
    private long _allocatedPosition;
    private long _startPosition;
    private long _endPosition;
    private bool _allocatedFlag;

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

        public void AddFirst(BufferSegment segment)
        {
            if (Head == null)
            {
                segment.PrevRef = null;
                segment.NextRef = null;
                Head = segment;
                Tail = segment;
            }
            else
            {
                // 4   head -> 1 -> 2 -> 3 <- tail
                // head -> 4 -> 1 -> 2 -> 3 <- tail
                Head.PrevRef = segment;
                segment.NextRef = Head;
                segment.PrevRef = null;
                Head = segment;
            }
            Count++;
        }

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
                segment.NextRef = null;
                segment.PrevRef = Tail;
                Tail.NextRef = segment;
                Tail = segment;
            }
            Count++;
        }

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

        public BufferSegment? PopLast()
        {
            if (Tail == null)
            {
                return null;
            }
            var tail = Tail;
            if (tail.PrevRef != null)
            {
                tail.PrevRef.NextRef = null;
            }
            Tail = Tail.PrevRef;
            Count--;
            if (Count == 0)
            {
                Head = null;
            }
            tail.NextRef = null;
            return tail;
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

        /// <summary>
        /// Absolute position of the first element withing the whole buffer.
        /// </summary>
        internal long AbsoluteStartPosition
        {
            get { return RunningIndex; }
            set { RunningIndex = value; }
        }

        /// <summary>
        /// Absolute position of the end element withing the whole buffer.
        /// </summary>
        internal long AbsoluteEndPosition { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferSegment(T[] buffer, long runningIndex = 0)
        {
            Buffer = buffer;
            Memory = new ReadOnlyMemory<T>(Buffer);
            RunningIndex = runningIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Contains(long position)
        {
            // There is a case when end of one buffer is the beginning of another buffer. So if
            // we have next buffer we consider that the current buffer doesn't contain the end position. Example:
            // 0_1_2_3 3_4_5_6 6_7_8_9
            // 9 = end of segment;
            // 3,6 = is not within the sequence because it is moved to next segment;
            if (_nextRef != null && position == AbsoluteEndPosition)
            {
                return false;
            }
            return position >= AbsoluteStartPosition && position <= AbsoluteEndPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DynamicBufferPosition GetPosition(long absolutePosition)
            => new(this, (int)(absolutePosition - AbsoluteStartPosition));

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

        internal long AbsolutePosition => _segment != null ? _offset + _segment.AbsoluteStartPosition : _offset;

        public bool Empty => _segment == null;

        public T? Value => _segment != null && _offset < _segment.Buffer.Length ? _segment.Buffer[_offset] : default;

        public static bool operator ==(DynamicBufferPosition left, DynamicBufferPosition right) => left.Equals(right);

        public static bool operator !=(DynamicBufferPosition left, DynamicBufferPosition right) => !(left == right);

        public static DynamicBufferPosition Null => new(null, 0);

        internal DynamicBufferPosition(object? segment, int offset)
        {
            _segment = (BufferSegment?)segment;
            _offset = offset;
        }

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is DynamicBufferPosition other && Equals(other);

        /// <inheritdoc />
        public bool Equals(DynamicBufferPosition other) => ReferenceEquals(_segment, other._segment) && _offset == other._offset;

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(_segment, _offset);

        /// <inheritdoc />
        public override string ToString() => $"Offset = {Offset} ({AbsolutePosition}), Segment = {_segment}";
    }

    private readonly struct SegmentChunk : IEquatable<SegmentChunk>
    {
        public BufferSegment Segment { get; }

        /// <summary>
        /// Start index within segment (local) of effective data.
        /// </summary>
        public int StartIndex { get; }

        /// <summary>
        /// End index within segment (local) of effective data.
        /// </summary>
        public int EndIndex { get; }

        /// <summary>
        /// Chunk size.
        /// </summary>
        public int Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => EndIndex - StartIndex;
        }

        public ReadOnlySpan<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Segment.Buffer, StartIndex, Size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SegmentChunk Empty() => new(BufferSegment.Empty, 0, 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SegmentChunk(BufferSegment segment, int startIndex, int endIndex)
        {
            Segment = segment;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        /// <inheritdoc />
        public bool Equals(SegmentChunk other)
            => Segment.Equals(other.Segment) && StartIndex == other.StartIndex && EndIndex == other.EndIndex;

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is SegmentChunk other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(Segment), StartIndex, EndIndex);
    }

    private ref struct ChunkIterator : IEnumerator<SegmentChunk>
    {
        private const int ModeStart = -1;
        private const int ModeNext = 0;
        private const int ModeNone = 1;

        private readonly DynamicBuffer<T> _dynamicBuffer;
        private readonly DynamicBufferPosition _startPosition;
        private readonly DynamicBufferPosition _endPosition;
        private BufferSegment? _currentSegment = BufferSegment.Empty;
        private int _mode = ModeStart;

        /// <inheritdoc />
        object? IEnumerator.Current => Current;

        public SegmentChunk Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_currentSegment == null)
                {
                    return SegmentChunk.Empty();
                }
                var segmentStartIndex = _currentSegment == _startPosition.Segment
                    ? _startPosition.Offset
                    : _dynamicBuffer.GetSegmentStartIndex(_currentSegment);
                var segmentEndIndex = _currentSegment == _endPosition.Segment
                    ? _endPosition.Offset
                    : _dynamicBuffer.GetSegmentEndIndex(_currentSegment);
                return new SegmentChunk(_currentSegment, segmentStartIndex, segmentEndIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ChunkIterator(DynamicBuffer<T> dynamicBuffer, DynamicBufferPosition start, DynamicBufferPosition end)
        {
            _dynamicBuffer = dynamicBuffer;
            _startPosition = start;
            _endPosition = end;
            _currentSegment = (BufferSegment?)_startPosition.Segment;
            _mode = _currentSegment != null ? ModeStart : ModeNone;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ChunkIterator(DynamicBuffer<T> dynamicBuffer, DynamicBufferPosition start)
            : this(dynamicBuffer, start, dynamicBuffer.End)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ChunkIterator(DynamicBuffer<T> dynamicBuffer)
            : this(dynamicBuffer, dynamicBuffer.Start, dynamicBuffer.End)
        {
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_mode == ModeStart)
            {
                _mode = ModeNext;
                return true;
            }

            if (_mode == ModeNone
                || _currentSegment == null
                || _currentSegment == _endPosition.Segment)
            {
                return false;
            }

            _currentSegment = _currentSegment.NextRef;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _currentSegment = (BufferSegment?)_startPosition.Segment;
            _mode = _currentSegment != null ? ModeStart : ModeNone;
        }

        public ChunkIterator GetEnumerator() => this;

        /// <inheritdoc />
        public void Dispose()
        {
        }
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
        _maxFreeBuffers = maxFreeBuffers;
    }

    /// <summary>
    /// Move the cursor by certain amount of elements.
    /// </summary>
    /// <param name="sizeToAdvance">Number of elements to move on.</param>
    /// <returns>Advanced elements count.</returns>
    public long Advance(long sizeToAdvance)
    {
        if (sizeToAdvance < 1)
        {
            return 0;
        }

        var advanced = Math.Min(sizeToAdvance, _size);
        _startPosition += advanced;
        UpdateSize();

        while (_buffersList.Head is { } head && head.AbsoluteEndPosition <= _startPosition)
        {
            _buffersList.PopFirst();
            if (_maxFreeBuffers < 0 || _freeBuffersList.Count < _maxFreeBuffers)
            {
                _freeBuffersList.AddLast(head);
            }
        }
        return advanced;
    }

    /// <summary>
    /// Moves the cursor to the end of the sequence.
    /// </summary>
    public void AdvanceToEnd()
    {
        while (_buffersList.PopFirst() is { } segment)
        {
            if (_maxFreeBuffers == -1 || _freeBuffersList.Count < _maxFreeBuffers)
            {
                _freeBuffersList.AddLast(segment);
            }
        }
        _allocatedPosition = 0;
        _startPosition = 0;
        _endPosition = 0;
        _allocatedFlag = false;
        _currentSegmentStartIndex = 0;
        _size = 0;
    }

    #region Read

    /// <summary>
    /// Allocate the buffer of <see cref="ChunkSize" /> amount
    /// of elements or return pre-existing buffer.
    /// </summary>
    /// <returns>Buffer.</returns>
    public Memory<T> Allocate()
    {
        EnsureNotAllocated();
        _allocatedFlag = true;
        return AllocateInternal();
    }

    private void EnsureNotAllocated()
    {
        if (_allocatedFlag)
        {
            throw new InvalidOperationException("You should commit data before allocate a new buffer.");
        }
    }

    private Memory<T> AllocateInternal()
    {
        // Check if we have spare space at current chunk.
        if (_buffersList.IsAny && _buffersList.Tail != null && _endPosition < _allocatedPosition)
        {
            var tailStartIndex = (int)(_endPosition - _buffersList.Tail.AbsoluteStartPosition);
            var bufferSize = _chunkSize - tailStartIndex;
            return _buffersList.Tail.Buffer.AsMemory(tailStartIndex, bufferSize);
        }

        return AddNextBufferSegment().Buffer;
    }

    private BufferSegment AddNextBufferSegment()
    {
        // Before allocate check if we have available free segment.
        var segment =_freeBuffersList.PopFirst();
        if (segment == null)
        {
            segment = new BufferSegment(GC.AllocateUninitializedArray<T>(_chunkSize));
        }

        _buffersList.AddLast(segment);
        segment.AbsoluteStartPosition = _currentSegmentStartIndex;
        _currentSegmentStartIndex += _chunkSize;
        segment.AbsoluteEndPosition = _currentSegmentStartIndex;
        _allocatedPosition += _chunkSize;
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
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_endPosition + size, _allocatedPosition);
        _allocatedFlag = false;
        if (size == 0)
        {
            CleanOrphanBuffers();
            return;
        }

        _endPosition += size;
        UpdateSize();
        Debug.Assert(_endPosition <= _allocatedPosition,
            "Allocated position cannot be before committed!");
    }

    private void CleanOrphanBuffers()
    {
        if (_buffersList.Tail != null
            && _buffersList.Tail.AbsoluteStartPosition >= _endPosition)
        {
            // If we allocated a new buffer but didn't write anything to it, we should free it.
            var segment = _buffersList.PopLast();
            if (segment != null && (_maxFreeBuffers == -1 || _freeBuffersList.Count < _maxFreeBuffers))
            {
                _freeBuffersList.AddLast(segment);
            }
            _allocatedPosition -= _chunkSize;
            _currentSegmentStartIndex -= _chunkSize;
        }
    }

    /// <summary>
    /// Get element at specific index.
    /// </summary>
    /// <param name="index">Element index.</param>
    /// <returns>Element.</returns>
    public T? GetAt(long index)
    {
        var success = TryGetAt(index, out var value);
        if (!success)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return value;
    }

    /// <summary>
    /// Try to get element by index.
    /// </summary>
    /// <param name="index">Element index.</param>
    /// <param name="value">Value or default.</param>
    /// <returns><c>True</c> if element can be reached, <c>false</c> otherwise.</returns>
    public bool TryGetAt(long index, out T? value)
    {
        if (index < 0 || index >= Size)
        {
            value = default;
            return false;
        }

        var indexPosition = GetPosition(index);
        foreach (var chunk in new ChunkIterator(this, indexPosition))
        {
            value = chunk.Segment.Buffer[chunk.StartIndex];
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>
    /// Get the position according to the given offset.
    /// </summary>
    /// <param name="offset">Position offset.</param>
    /// <returns>Instance of <see cref="DynamicBufferPosition" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DynamicBufferPosition GetPosition(long offset) => offset > 0 ? GetPosition(offset, Start) : Start;

    /// <summary>
    /// Get the position according to the given offset.
    /// </summary>
    /// <param name="offset">Position offset.</param>
    /// <param name="bufferPosition">Position to get offset of.</param>
    /// <returns>Instance of <see cref="DynamicBufferPosition" />.</returns>
    public DynamicBufferPosition GetPosition(long offset, DynamicBufferPosition bufferPosition)
    {
        if (offset == 0)
        {
            return bufferPosition;
        }
        var currentSegment = (BufferSegment?)bufferPosition.Segment;
        if (currentSegment == null)
        {
            return bufferPosition;
        }

        var position = currentSegment.AbsoluteStartPosition + bufferPosition.Offset;
        var targetPosition = position + offset;

        if (targetPosition <= _startPosition)
        {
            return Start;
        }
        if (targetPosition >= _endPosition)
        {
            return End;
        }

        while (currentSegment != null)
        {
            if (currentSegment.Contains(targetPosition))
            {
                return currentSegment.GetPosition(targetPosition);
            }
            currentSegment = offset > 0 ? currentSegment.NextRef : currentSegment.PrevRef;
        }

        // Shouldn't be here. Exceptional safe case.
        return DynamicBufferPosition.Null;
    }

    /// <summary>
    /// Get data between start and end indexes.
    /// It returns a live view or a new buffer with the data copied from the dynamic buffer.
    /// </summary>
    /// <param name="startIndex">Start index.</param>
    /// <param name="endIndex">End index. -1 is to read the entire buffer to the end.</param>
    /// <returns>Span. Notice that target length is endIndex-startIndex.</returns>
    public ReadOnlySpan<T> Slice(long startIndex, long endIndex = -1)
    {
        var startPosition = GetPosition(startIndex);
        var endPosition = endIndex > -1 ? GetPosition(endIndex - startIndex, startPosition) : End;
        return Slice(startPosition, endPosition);
    }

    /// <summary>
    /// Get data between start and end positions.
    /// It returns a live view or a new buffer with the data copied from the dynamic buffer.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="length">Length of the target span.</param>
    /// <returns>Span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> Slice(DynamicBufferPosition start, long length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, Size);

        var startSegment = (BufferSegment?)start.Segment;
        if (startSegment == null)
        {
            return [];
        }

        // Fast case.
        if (length <= _chunkSize - start.Offset)
        {
            length = Math.Min(length, _endPosition - start.AbsolutePosition);
            return length > 0 ? new ReadOnlySpan<T>(startSegment.Buffer, start.Offset, (int)length) : [];
        }

        // Slow path.
        return SliceSlow(start, length);
    }

    private ReadOnlySpan<T> SliceSlow(DynamicBufferPosition start, long length)
    {
        var startSegment = (BufferSegment?)start.Segment;
        var targetPosition = start.AbsolutePosition + length;
        var currentSegment = startSegment;
        var end = End;
        while (currentSegment != null)
        {
            if (currentSegment.Contains(targetPosition))
            {
                end = currentSegment.GetPosition(targetPosition);
                break;
            }
            currentSegment = currentSegment.NextRef;
        }

        return Slice(start, end);
    }

    /// <summary>
    /// Get data between start and end positions.
    /// It returns a live view or a new buffer with the data copied from the dynamic buffer.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="end">End position.</param>
    /// <returns>Span.</returns>
    public ReadOnlySpan<T> Slice(DynamicBufferPosition start, DynamicBufferPosition end)
    {
        var startSegment = (BufferSegment?)start.Segment;
        var endSegment = (BufferSegment?)end.Segment;
        if (startSegment == null || endSegment == null)
        {
            return [];
        }

        // Fast case.
        if (startSegment == endSegment)
        {
            var length = end.Offset - start.Offset;
            if (length < 1)
            {
                return [];
            }
            return new ReadOnlySpan<T>(startSegment.Buffer, start.Offset, length);
        }

        // Calculate target buffer size.
        var size = (int)(endSegment.AbsoluteStartPosition - startSegment.AbsoluteStartPosition + end.Offset - start.Offset);
        if (size < 1)
        {
            return [];
        }

        // Fill buffer and return.
        var localBuffer = new T[size];
        var offset = 0;
        foreach (var chunk in new ChunkIterator(this, start, end))
        {
            var span = chunk.Span;
            span.CopyTo(localBuffer.AsSpan(offset));
            offset += span.Length;
        }

        return localBuffer;
    }

    /// <summary>
    /// Get the first index of any specified values.
    /// </summary>
    /// <param name="values">The values to look for.</param>
    /// <param name="foundValue">Found value.</param>
    /// <param name="skip">Start index to search from. Default is 0.</param>
    /// <returns>The value index or -1 if not found.</returns>
    public long IndexOfAny(scoped ReadOnlySpan<T> values, out T? foundValue, long skip = 0)
    {
        var startPosition = GetPosition(skip);
        foreach (var chunk in new ChunkIterator(this, startPosition))
        {
            var span = chunk.Span;
            var foundValueIndex = span.IndexOfAny(values);
            if (foundValueIndex > -1)
            {
                foundValue = span[foundValueIndex];
                return foundValueIndex + chunk.StartIndex + chunk.Segment.AbsoluteStartPosition - _startPosition;
            }
        }

        foundValue = default;
        return -1;
    }

    /// <summary>
    /// Get the first index of any specified values.
    /// </summary>
    /// <param name="values">The values to look for.</param>
    /// <param name="foundValue">Found value.</param>
    /// <param name="skip">Start index to search from. Default is 0.</param>
    /// <returns>The value index or -1 if not found.</returns>
    public long IndexOfAny(SearchValues<T> values, out T? foundValue, long skip = 0)
    {
        var startPosition = GetPosition(skip);
        foreach (var chunk in new ChunkIterator(this, startPosition))
        {
            var span = chunk.Span;
            var foundValueIndex = span.IndexOfAny(values);
            if (foundValueIndex > -1)
            {
                foundValue = span[foundValueIndex];
                return foundValueIndex + chunk.StartIndex + chunk.Segment.AbsoluteStartPosition - _startPosition;
            }
        }

        foundValue = default;
        return -1;
    }

    #region Segment position

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentStartIndex(BufferSegment? bufferSegment)
        => bufferSegment != null && bufferSegment == _buffersList.Head
            ? (int)(_startPosition - bufferSegment.AbsoluteStartPosition)
            : 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentEndIndex(BufferSegment? bufferSegment)
        => bufferSegment != null && bufferSegment == _buffersList.Tail
            ? (int)(_endPosition - bufferSegment.AbsoluteStartPosition)
            : _chunkSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetSegmentLength(BufferSegment? bufferSegment)
    {
        var startIndex = GetSegmentStartIndex(bufferSegment);
        var endIndex = GetSegmentEndIndex(bufferSegment);
        return endIndex - startIndex;
    }

    #endregion

    /// <summary>
    /// Get the total buffer without advanced data.
    /// </summary>
    /// <returns>The total sequence.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySequence<T> GetSequence()
    {
        if (_buffersList.Head == null || _buffersList.Tail == null || IsEmpty)
        {
            return ReadOnlySequence<T>.Empty;
        }
        var headStartIndex = GetSegmentStartIndex(_buffersList.Head);
        var tailEndIndex = (int)(_endPosition - _buffersList.Tail.AbsoluteStartPosition);
        return new ReadOnlySequence<T>(_buffersList.Head, headStartIndex, _buffersList.Tail, tailEndIndex);
    }

    /// <summary>
    /// Get the total buffer without advanced data.
    /// </summary>
    /// <param name="start">Start position.</param>
    /// <param name="length">Length of the sequence.</param>
    /// <returns>The total sequence.</returns>
    public ReadOnlySequence<T> GetSequence(DynamicBufferPosition start, long length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (_buffersList.Head == null || IsEmpty || length == 0)
        {
            return ReadOnlySequence<T>.Empty;
        }

        var targetStartPosition = start.AbsolutePosition;
        var targetEndPosition = start.AbsolutePosition + length;
        BufferSegment? startSegment = null;
        var startIndex = -1;

        foreach (var chunk in new ChunkIterator(this, start))
        {
            if (chunk.Segment.Contains(targetStartPosition))
            {
                startSegment = chunk.Segment;
                startIndex = (int)(targetStartPosition - chunk.Segment.AbsoluteStartPosition);
            }
            if (chunk.Segment.Contains(targetEndPosition) && startSegment != null)
            {
                var endSegment = chunk.Segment;
                var endIndex = (int)(targetEndPosition - chunk.Segment.AbsoluteStartPosition);
                return new ReadOnlySequence<T>(startSegment, startIndex, endSegment, endIndex);
            }
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
        var endSegment = (BufferSegment?)end.Segment;
        if (startSegment == null || endSegment == null)
        {
            return ReadOnlySequence<T>.Empty;
        }

        return new ReadOnlySequence<T>(startSegment, start.Offset, endSegment, end.Offset);
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
            foreach (var chunk in new ChunkIterator(this, Start))
            {
                var span = chunk.Span;
                if (span.Length > buffer.Length)
                {
                    span = span.Slice(0, buffer.Length);
                }
                span.CopyTo(buffer);
                buffer = buffer.Slice(span.Length);
                totalRead += span.Length;
            }
        }

        var success = totalRead == bufferSize;
        if (success && advance)
        {
            Advance(totalRead);
        }

        return success;
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
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (IsEmpty || count > Size)
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
            var newBuffer = GC.AllocateUninitializedArray<T>(Size > count ? count : (int)Size);
            sequence.Slice(0, count).CopyTo(newBuffer);
            buffer = newBuffer;
        }

        var success = buffer.Length == count;
        if (success && advance)
        {
            Advance(buffer.Length);
        }

        return success;
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
        ArgumentOutOfRangeException.ThrowIfNegative(repeat);
        if (repeat < 1)
        {
            return;
        }

        EnsureNotAllocated();
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
        EnsureNotAllocated();
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

            var position = (int)(_endPosition % _chunkSize);
            var upperIndex = remainBuffer > data.Length - writeIndex ? data.Length : remainBuffer + writeIndex;
            data[writeIndex..upperIndex].CopyTo(buffer.Span[position..]);
            var append = upperIndex - writeIndex;
            _endPosition += append;
            writeIndex += append;
        }
        UpdateSize();
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
    private void UpdateSize() => _size = _endPosition - _startPosition;

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
