using Xunit;
using QueryCat.Plugins.Client;

namespace QueryCat.UnitTests.Utils;

/// <summary>
/// Tests for <see cref="SimpleUri" />.
/// </summary>
public class SimpleUriTests
{
    [Theory]
    [InlineData("http://dzen.ru/", "dzen.ru")]
    [InlineData("http://*:2121/", "*")]
    [InlineData("ftp://host.com/pub/", "host.com")]
    public void Uri_Host_ShouldMatch(string uri, string target)
    {
        var simpleUri = new SimpleUri(uri);
        Assert.Equal(target, simpleUri.Host);
    }

    [Theory]
    [InlineData("http://dzen.ru/", "http")]
    [InlineData("ftp://host.com/pub/", "ftp")]
    public void Uri_Scheme_ShouldMatch(string uri, string target)
    {
        var simpleUri = new SimpleUri(uri);
        Assert.Equal(target, simpleUri.Scheme);
    }

    [Theory]
    [InlineData("http://dzen.ru/", -1)]
    [InlineData("ftp://host.com:21/pub/", 21)]
    public void Uri_Port_ShouldMatch(string uri, int target)
    {
        var simpleUri = new SimpleUri(uri);
        Assert.Equal(target, simpleUri.Port);
    }

    [Theory]
    [InlineData("http://dzen.ru", new[] { "/" })]
    [InlineData("http://dzen.ru/", new[] { "/" })]
    [InlineData("ftp://host.com:21/pub", new[] { "/", "pub" })]
    [InlineData("ftp://host.com/pub/sub/", new[] { "/", "pub/", "sub/" })]
    public void Uri_Segments_ShouldMatch(string uri, string[] target)
    {
        var simpleUri = new SimpleUri(uri);
        Assert.Equal(target, simpleUri.Segments);
    }
}
