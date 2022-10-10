using System.Text;
using Xunit;
using QueryCat.Backend.Storage.Formats;
using QueryCat.Backend.Types;

namespace QueryCat.IntegrationTests;

/// <summary>
/// Tests for <see cref="DsvInput" />.
/// </summary>
public class StreamRowsInputTests
{
    [Fact]
    public void ReadNextWithDelimiters_CsvWithWindowsNewLines_ShouldParseCorrect()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id,name\r\n")
            .Append("10,john");

        // Act.
        var streamRowsInput = new DsvInput(StringToStream(sb.ToString()), ',');
        streamRowsInput.Open();
        streamRowsInput.ReadNext();

        // Assert.
        streamRowsInput.ReadValue(0, out VariantValue value);
        Assert.Equal(10, value.AsInteger);
        streamRowsInput.ReadValue(1, out value);
        Assert.Equal("john", value.AsString);
    }

    [Fact]
    public void ReadNextWithDelimiters_CsvWithUnixNewLines_ShouldParseCorrect()
    {
        // Arrange.
        var sb = new StringBuilder()
            .Append("id,name\n")
            .Append("10,john\n");

        // Act.
        var streamRowsInput = new DsvInput(StringToStream(sb.ToString()), ',');
        streamRowsInput.Open();
        streamRowsInput.ReadNext();

        // Assert.
        streamRowsInput.ReadValue(0, out VariantValue value);
        Assert.Equal(10, value.AsInteger);
        streamRowsInput.ReadValue(1, out value);
        Assert.Equal("john", value.AsString);
    }

    private static Stream StringToStream(string value)
        => new MemoryStream(Encoding.UTF8.GetBytes(value));
}
