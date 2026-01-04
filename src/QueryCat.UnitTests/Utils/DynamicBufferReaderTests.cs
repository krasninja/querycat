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
        dynamicBuffer.Write("1234567890");
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
        dynamicBuffer.Write("1234567890");
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.Advance(999);

        // Assert.
        Assert.Equal('0', reader.Current);
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
        dynamicBuffer.Write("1234567890");
        var reader = new DynamicBuffer<char>.DynamicBufferReader(dynamicBuffer);

        // Act.
        reader.Advance(8);
        reader.Rewind(8);

        // Assert.
        Assert.Equal('1', reader.Current);
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
}
