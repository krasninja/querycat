using System.Text;
using Xunit.Abstractions;

namespace QueryCat.UnitTests;

/// <summary>
/// Catch stdout and redirect to XUnit test output.
/// </summary>
internal sealed class TestOutputWriter : TextWriter
{
    private readonly ITestOutputHelper _output;

    public TestOutputWriter(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <inheritdoc />
    public override Encoding Encoding => Encoding.UTF8;

    /// <inheritdoc />
    public override void WriteLine(string? message)
    {
        _output.WriteLine(message);
    }

    /// <inheritdoc />
    public override void Write(char[] buffer, int index, int count)
    {
        _output.WriteLine(new string(buffer, index, count));
    }
}
