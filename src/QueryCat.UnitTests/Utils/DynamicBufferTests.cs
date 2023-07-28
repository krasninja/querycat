using Xunit;
using QueryCat.Backend.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="DynamicBuffer{T}" />.
/// </summary>
public class DynamicBufferTests
{
    [Fact]
    public void Commit_CommitOneSegment_ShouldIncreaseBuffer()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<byte>(chunkSize: 10);

        // Act.
        var buffer = dynamicBuffer.Allocate();
        dynamicBuffer.Commit(buffer);

        // Assert.
        Assert.Equal(dynamicBuffer.ChunkSize, dynamicBuffer.Size);
        Assert.Equal(1, dynamicBuffer.TotalBuffersCount);
        Assert.Equal(1, dynamicBuffer.UsedBuffersCount);
    }

    [Fact]
    public void Advance_PartialAdvance_ShouldDecreaseSpecificSize()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<byte>(chunkSize: 10);

        // Arrange.
        for (var i = 0; i < 3; i++)
        {
            var buffer = dynamicBuffer.Allocate();
            dynamicBuffer.Commit(buffer);
        }

        // Act.
        dynamicBuffer.Advance(1);
        dynamicBuffer.Advance(4);
        dynamicBuffer.Advance(5);
        dynamicBuffer.Advance(10);
        dynamicBuffer.Advance(5);

        // Assert.
        Assert.Equal(5, dynamicBuffer.Size);
    }

    [Fact]
    public void Advance_MoreBytesThanAvailable_ZeroSize()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<byte>(chunkSize: 10);
        var buffer = dynamicBuffer.Allocate();
        dynamicBuffer.Commit(buffer);

        // Act.
        dynamicBuffer.Advance(1000);

        // Assert.
        Assert.Equal(0, dynamicBuffer.Size);
    }

    [Fact]
    public void GetSequence_CommitOneSegment_ShouldReturnOne()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<byte>(chunkSize: 10);

        // Act.
        var buffer = dynamicBuffer.Allocate();
        buffer[0] = 1;
        buffer[1] = 2;
        dynamicBuffer.Commit(2);
        buffer = dynamicBuffer.Allocate();
        buffer[0] = 3;
        buffer[1] = 4;
        dynamicBuffer.Commit(2);

        // Assert.
        var resultBuffer = dynamicBuffer.GetSequence();
        Assert.Equal(1, resultBuffer.GetElementAt(0));
        Assert.Equal(4, resultBuffer.GetElementAt(3));
        Assert.Equal(1, dynamicBuffer.TotalBuffersCount);
        Assert.Equal(1, dynamicBuffer.UsedBuffersCount);
    }

    [Fact]
    public void GetSequence_CommitPartialSegment_ReturnRemain()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<byte>(chunkSize: 60);

        // Act.
        var buffer = dynamicBuffer.Allocate();
        buffer[4] = 10;
        dynamicBuffer.Commit(40);
        dynamicBuffer.Advance(4);

        // Assert.
        Assert.Equal(10, dynamicBuffer.GetSequence().FirstSpan[0]);
        Assert.Equal(40 - 4, dynamicBuffer.GetSequence().Length);
        Assert.Equal(40 - 4, dynamicBuffer.Size);
    }

    [Fact]
    public void GetSequence_AdvancedBuffer_ShouldStartFromZero()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<byte>(chunkSize: 10);
        var buffer = dynamicBuffer.Allocate();
        buffer.Fill(1);
        dynamicBuffer.Commit(buffer);
        buffer = dynamicBuffer.Allocate();
        buffer.Fill(2);
        dynamicBuffer.Commit(5);
        dynamicBuffer.Advance(5);

        // Act.
        var sequence = dynamicBuffer.GetSequence();

        // Assert.
        Assert.Equal(10, sequence.Length);
        Assert.Equal(1, sequence.GetElementAt(0));
        Assert.Equal(2, sequence.GetElementAt(5));
        Assert.Equal(2, dynamicBuffer.TotalBuffersCount);
        Assert.Equal(2, dynamicBuffer.UsedBuffersCount);
    }

    [Fact]
    public void Allocate_NewBuffer_ShouldReuseExisting()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<byte>(chunkSize: 10);
        for (var i = 0; i < 3; i++)
        {
            var buffer = dynamicBuffer.Allocate();
            dynamicBuffer.Commit(buffer);
        }

        // Act.
        dynamicBuffer.Advance(dynamicBuffer.ChunkSize + 5);
        dynamicBuffer.Allocate();
        dynamicBuffer.Commit(dynamicBuffer.ChunkSize);

        // Assert.
        Assert.Equal(3, dynamicBuffer.TotalBuffersCount);
        Assert.Equal(dynamicBuffer.ChunkSize * 3 - 5, dynamicBuffer.Size);
    }

    [Fact]
    public void Allocate_ExistingBuffer_ShouldReuseCorrect()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<byte>(chunkSize: 10);
        for (byte i = 0; i < 3; i++)
        {
            var buffer = dynamicBuffer.Allocate();
            buffer.Fill(i);
            dynamicBuffer.Commit(buffer);
        }

        // Act.
        dynamicBuffer.Advance(dynamicBuffer.Size);
        for (byte i = 0; i < 2; i++)
        {
            var buffer = dynamicBuffer.Allocate();
            buffer.Fill(42);
            dynamicBuffer.Commit(buffer);
        }

        // Assert.
        var sequence = dynamicBuffer.GetSequence();
        Assert.Equal(dynamicBuffer.ChunkSize * 2, dynamicBuffer.Size);
        Assert.Equal(42, sequence.GetElementAt(0));
    }

    [Fact]
    public void IndexOfAny_TwoDelimiters_ShouldFindFirst()
    {
        /*
         * 1234,56;78 9_________
         * 0123456789 0123456789
         * 0          10
         */

        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 10);
        dynamicBuffer.Write("1234,56;789");
        var delimiters = new[] { ',', ';' };

        // Act.
        var firstIndex = dynamicBuffer.IndexOfAny(delimiters, out _);
        dynamicBuffer.Advance(firstIndex);
        dynamicBuffer.Advance(1);
        var secondIndex = dynamicBuffer.IndexOfAny(delimiters, out _);
        dynamicBuffer.Advance(secondIndex);
        dynamicBuffer.Advance(1);
        var thirdIndex = dynamicBuffer.IndexOfAny(delimiters, out _);

        // Assert.
        Assert.Equal(4, firstIndex);
        Assert.Equal(2, secondIndex);
        Assert.Equal(-1, thirdIndex);
    }

    [Fact]
    public void IndexOfAny_7SegmentsAnd3Delimiters_ShouldFindAll()
    {
        /*
         * ,____ __abc de,fg hijkl mn;op qrst, _____
         * 01234 01234 01234 01234 01234 01234 01234
         * 0     5     10    15    20    25    30
         */

        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 5);
        dynamicBuffer.Write(",______abcde,fghijklmn;opqrst,_____");
        dynamicBuffer.Advance(1); // Here we have +1 shift now!
        var delimiters = new[] { ',', ';' };

        // Act.
        var firstIndex = dynamicBuffer.IndexOfAny(delimiters, out _);
        var secondIndex = dynamicBuffer.IndexOfAny(delimiters, out var secondDelimiter, firstIndex + 1);
        var thirdIndex = dynamicBuffer.IndexOfAny(delimiters, out var thirdDelimiter, secondIndex + 1);
        dynamicBuffer.Advance(secondIndex);
        var fourthIndex = dynamicBuffer.IndexOfAny(delimiters, out _);

        // Assert.
        Assert.Equal(11, firstIndex);
        Assert.Equal(21, secondIndex);
        Assert.Equal(';', secondDelimiter);
        Assert.Equal(28, thirdIndex);
        Assert.Equal(',', thirdDelimiter);
        Assert.Equal(0, fourthIndex);
    }

    [Fact]
    public void IndexOfAny_2SegmentsAnd2Delimiters_ShouldFindNearest()
    {
        /*
         * ;_cd;,ef,h
         * 0123456789
         * 0
         */

        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 10);
        dynamicBuffer.Write(";_cd;,ef,h");
        dynamicBuffer.Advance(2); // Here we have +2 shift now!
        var delimiters = new[] { ',', ';' };

        // Act.
        var firstIndex = dynamicBuffer.IndexOfAny(delimiters, out var firstDelimiter);

        // Assert.
        Assert.Equal(2, firstIndex);
        Assert.Equal(';', firstDelimiter);
    }

    [Fact]
    public void IndexOfAny_EmptyBuffer_ShouldFindNoting()
    {
        /*
         * ;_cd;,ef,h 0123456789
         * 0123456789 0123456789
         * 0          10
         */

        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 10);
        dynamicBuffer.Write(";_cd;,ef,h0123456789");
        dynamicBuffer.Advance(20);
        var delimiters = new[] { ',', ';' };

        // Act.
        var firstIndex = dynamicBuffer.IndexOfAny(delimiters, out _);

        // Assert.
        Assert.Equal(-1, firstIndex);
    }

    [Fact]
    public void GetSpan_VariousCases_ShouldReturnCorrectSpan()
    {
        /*
         * xxxxx abcde fghij klmno pqrst
         * 01234 01234 01234 01234 01234
         *       0     5     10    15
         */

        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 5);
        dynamicBuffer.Write("xxxxxabcdefghijklmnopqrst");
        dynamicBuffer.Advance(5); // Take -5!

        // Act.
        var span1 = dynamicBuffer.GetSpan(1, 4);
        var span2 = dynamicBuffer.GetSpan(13, 17);
        var span3 = dynamicBuffer.GetSpan(3, 14);
        dynamicBuffer.Advance(3);
        var span4 = dynamicBuffer.GetSpan(3, 4);

        // Assert.
        Assert.Equal("bcd", span1.ToString());
        Assert.Equal("nopq", span2.ToString());
        Assert.Equal("defghijklmn", span3.ToString());
        Assert.Equal("g", span4.ToString());
    }

    [Fact]
    public void TryCopyExact_BufferWithData_ShouldCopy()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 5);
        dynamicBuffer.Write("123456789");

        // Act.
        var outputBuffer = new char[3];
        var success = dynamicBuffer.TryCopyExact(outputBuffer.AsSpan(), advance: true);

        // Assert.
        Assert.True(success);
        Assert.Equal("123", outputBuffer);
    }

    [Fact]
    public void TryReadExact_BufferWithData_ShouldRead()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 3);
        dynamicBuffer.Write("01234567890123456789");
        dynamicBuffer.Advance(11); // 123456789

        // Act.
        var success = dynamicBuffer.TryReadExact(3, out var outputBuffer, advance: true);

        // Assert.
        Assert.True(success);
        Assert.Equal("123", outputBuffer.ToArray());
    }

    [Fact]
    public void Write_DataOverBuffer_ShouldExpand()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 5);

        // Act.
        dynamicBuffer.Write('a', 12);
        dynamicBuffer.Advance(2);

        // Assert.
        Assert.Equal(10, dynamicBuffer.GetSequence().Length);
    }

    [Fact]
    public void Write_ArrayOverBuffer_ShouldExpand()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 5);

        // Act.
        dynamicBuffer.Write("123451234567");

        // Assert.
        Assert.Equal(12, dynamicBuffer.GetSequence().Length);
    }

    [Fact]
    public void Write_ArrayOverBufferWithPadding_ShouldExpandWithPadding()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 5);

        // Act.
        dynamicBuffer.WritePadRight("1234567890123", 15, '_');

        // Assert.
        Assert.Equal("1234567890123__", dynamicBuffer.GetSequence().ToString());
    }

    [Fact]
    public void Write_ArrayOverBufferAdvanceAndWrite_ShouldExpand()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 5);

        // Act.
        dynamicBuffer.Write("12345678");
        dynamicBuffer.Advance(3);
        dynamicBuffer.Write("90");

        // Assert.
        Assert.Equal("4567890", dynamicBuffer.GetSequence().ToString());
    }

    [Fact]
    public void Write_ArrayOverBufferAdvanceAndWrite2_ShouldExpand()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 32);

        // Act.
        dynamicBuffer.Write("123456789012345"); // 15 items.
        dynamicBuffer.Advance(15);
        dynamicBuffer.Write("123456789012345"); // 15 items.
        dynamicBuffer.Advance(15);

        // Assert.
        Assert.Empty(dynamicBuffer.GetSequence().ToString());
    }

    [Fact]
    public void Allocate_MultipleCopy_ShouldConcatenate()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 5);

        // Act.
        var buf1 = dynamicBuffer.Allocate();
        "12".CopyTo(buf1);
        "34".CopyTo(buf1.Slice(2, 2));
        dynamicBuffer.Commit(2);
        dynamicBuffer.Commit(2);

        // Assert.
        Assert.Equal("1234", dynamicBuffer.GetSequence().ToString());
        Assert.Equal("1234", dynamicBuffer.GetSpan(0).ToString());
    }
}
