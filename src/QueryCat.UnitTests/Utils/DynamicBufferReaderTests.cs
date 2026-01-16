using Xunit;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="DynamicBuffer{T}.DynamicBufferReader" />.
/// </summary>
public class DynamicBufferReaderTests
{
    [Fact]
    public void Advance_Position_ShouldAdvance()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 5);
        dynamicBuffer.Write("1234567890"); // 2345 | 67890
        dynamicBuffer.Advance(1);
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.Advance(8);

        // Assert.
        Assert.Equal('0', reader.Current);
    }

    [Fact]
    public void Advance_PositionExceedBuffer_ShouldAdvanceToEnd()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 3);
        dynamicBuffer.Write("1234567890"); // 123 | 456 | 789 | 0
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.Advance(999);

        // Assert.
        Assert.Equal('0', reader.Current);
    }

    [Fact]
    public void Advance_AtTheEnd_ShouldNotAdvance()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 4);
        dynamicBuffer.Write("1234");
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);
        reader.Advance(3);

        // Act.
        reader.Advance(1);

        // Assert.
        Assert.Equal('4', reader.Current);
    }

    [Fact]
    public void Advance_PositionWithAdvance_ShouldCalculateRemainBufferCorrectly()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 4);
        dynamicBuffer.Write("1234567890"); // 1234 | 5678 | 90
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);
        reader.Advance(3);

        // Act.
        reader.Advance(2);

        // Assert.
        Assert.Equal('6', reader.Current);
    }

    [Fact]
    public void AdvancePastAny_CharToSkip_ShouldAdvancePast()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 4);
        dynamicBuffer.Write("001223344599");
        dynamicBuffer.Advance(2);
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.AdvancePastAny("123456");

        // Assert.
        Assert.Equal('9', reader.Current);
    }

    [Fact]
    public void AdvanceToEnd_MoveToEndOfStream_ShouldBeAtEnd()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 4);
        dynamicBuffer.Write("1234567890"); // 1234 | 5678 | 90
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.AdvanceToEnd();

        // Assert.
        Assert.Equal('0', reader.Current);
        Assert.Equal(dynamicBuffer.End, reader.Position);
    }

    [Fact]
    public void TryAdvanceToAny_CharsToSeek_ShouldAdvancePast()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 4);
        dynamicBuffer.Write("1234567890_1234567890");
        dynamicBuffer.Advance(3);
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.TryAdvanceToAny("_0", advancePastDelimiter: false);

        // Assert.
        Assert.Equal('0', reader.Current);
    }

    [Fact]
    public void TryAdvanceToAnyWithPast_CharsToSeek_ShouldAdvancePast()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 4);
        dynamicBuffer.Write("111122203333");
        dynamicBuffer.Advance(4);
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.TryAdvanceToAny("0", advancePastDelimiter: true);

        // Assert.
        Assert.Equal('3', reader.Current);
    }

    [Fact]
    public void TryAdvanceWithPast_CharToSeek_ShouldAdvancePast()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 4);
        dynamicBuffer.Write("111122223333");
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.TryAdvanceTo('2', advancePastDelimiter: true);

        // Assert.
        Assert.Equal('2', reader.Current);
    }

    [Fact]
    public void Rewind_ToPreviousPosition_ShouldRewind()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 3);
        dynamicBuffer.Write("1234567890"); // 123 | 456 | 789 | 0
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.Advance(8);
        reader.Rewind(8);

        // Assert.
        Assert.Equal('1', reader.Current);
    }

    [Fact]
    public void Rewind_WithinSegment_ShouldRewind()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>();
        dynamicBuffer.Write("001234567890");
        dynamicBuffer.Advance(2);
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.Advance(8);
        reader.Rewind(7);

        // Assert.
        Assert.Equal('2', reader.Current);
    }

    [Fact]
    public void IsNext_Char_ShouldReturnMatch()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 4);
        dynamicBuffer.Write("111122223333");
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.Advance(3);

        // Assert.
        Assert.True(reader.IsNext('2'));
    }

    [Fact]
    public void GetPosition_NextPrevPositions_ShouldGetCorrect()
    {
        // Arrange.
        var dynamicBuffer = new DynamicBuffer<char>(chunkSize: 3);
        dynamicBuffer.Write("00_123456789");
        /*
         * 0 0 _ | 1 2 3 | 4 5 6 | 7  8  9
         * 0 1 2 | 3 4 5 | 6 7 8 | 9  10 11
         */
        dynamicBuffer.Advance(3);
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.Advance(5);

        // Assert.
        Assert.Equal('6', reader.Position.Value);
        Assert.Equal('1', reader.GetPosition(-5).Value);
        Assert.Equal('8', reader.GetPosition(2).Value);
        Assert.Equal('9', reader.GetPosition(666).Value);
        Assert.Equal('1', reader.GetPosition(-666).Value);
    }
}
