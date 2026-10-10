using Xunit;
using QueryCat.Backend.Utils;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="StreamWrapper" />.
/// </summary>
public class StreamWrapperTests
{
    [Fact]
    public void Read_TwoWrappersOverSameStream_ShouldHaveIndependentPositions()
    {
        // Arrange.
        var stream = new MemoryStream([1, 2, 3, 4, 5]);
        using var wrapper1 = new StreamWrapper(stream, leaveOpen: true);
        using var wrapper2 = new StreamWrapper(stream, leaveOpen: true);
        var buffer1 = new byte[2];
        var buffer2 = new byte[3];

        // Act.
        wrapper1.ReadExactly(buffer1);
        wrapper2.ReadExactly(buffer2);
        var nextByte = wrapper1.ReadByte();

        // Assert.
        Assert.Equal(new byte[] { 1, 2 }, buffer1);
        Assert.Equal(new byte[] { 1, 2, 3 }, buffer2);
        Assert.Equal(3, nextByte);
        Assert.Equal(3, wrapper1.Position);
        Assert.Equal(3, wrapper2.Position);
    }

    [Fact]
    public async Task ReadAsync_AfterSeek_ShouldReadFromWrapperPosition()
    {
        // Arrange.
        var stream = new MemoryStream([1, 2, 3, 4, 5]);
        await using var wrapper = new StreamWrapper(stream, leaveOpen: true);
        var buffer = new byte[2];

        // Act.
        wrapper.Seek(-2, SeekOrigin.End);
        stream.Position = 0;
        await wrapper.ReadExactlyAsync(buffer);

        // Assert.
        Assert.Equal(new byte[] { 4, 5 }, buffer);
        Assert.Equal(5, wrapper.Position);
    }

    [Fact]
    public void Seek_BeforeBeginning_ShouldThrow()
    {
        // Arrange.
        using var wrapper = new StreamWrapper(new MemoryStream([1, 2, 3]));

        // Act and assert.
        Assert.Throws<IOException>(() => wrapper.Seek(-1, SeekOrigin.Begin));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Dispose_LeaveOpen_ShouldRespectFlag(bool leaveOpen)
    {
        // Arrange.
        var stream = new MemoryStream([1, 2, 3]);
        var wrapper = new StreamWrapper(stream, leaveOpen);

        // Act.
        wrapper.Dispose();

        // Assert.
        Assert.Equal(leaveOpen, stream.CanRead);
        Assert.Throws<ObjectDisposedException>(() => wrapper.ReadByte());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisposeAsync_LeaveOpen_ShouldRespectFlag(bool leaveOpen)
    {
        // Arrange.
        var stream = new MemoryStream([1, 2, 3]);
        var wrapper = new StreamWrapper(stream, leaveOpen);

        // Act.
        await wrapper.DisposeAsync();

        // Assert.
        Assert.Equal(leaveOpen, stream.CanRead);
    }
}
