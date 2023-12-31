using System.Buffers;
using System.Text;
using QueryCat.Backend.Core.Types.Blob;
using Xunit;

namespace QueryCat.UnitTests.Types;

/// <summary>
/// Tests for <see cref="BytesBlobData" />.
/// </summary>
public class BytesBlobDataTests
{
    [Fact]
    public void GetBytes_PartOfArray_ShouldReturn()
    {
        // Arrange.
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var blobData = new BytesBlobData(bytes);

        // Act.
        var newArr = new byte[4];
        var readBytes = blobData.GetBytes(newArr, 1, 30);

        // Assert.
        Assert.Equal(4, readBytes);
        Assert.Equal(new byte[] { 2, 3, 4, 5 }, newArr);
    }

    [Fact]
    public void ApplyAction_Combine_ShouldCombine()
    {
        // Arrange.
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var blobData = new BytesBlobData(bytes);

        // Act.
        var sb = new StringBuilder();
        var readBytes = blobData.ApplyAction(new ReadOnlySpanAction<byte, object?>((span, arg) =>
        {
            foreach (var s in span)
            {
                sb.Append(s);
            }
        }), 1);

        // Assert.
        Assert.Equal(4, readBytes);
        Assert.Equal("2345", sb.ToString());
    }
}
