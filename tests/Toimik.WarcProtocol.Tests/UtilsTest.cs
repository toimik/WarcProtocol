namespace Toimik.WarcProtocol.Tests;

using System;
using Xunit;

public class UtilsTest
{
    [Fact]
    public void AddBracketsToUriWithUriThatIsNull()
    {
        Uri? uri = null;
        var actualUri = Utils.AddBracketsToUri(uri);

        Assert.Null(actualUri);
    }

    [Theory]
    [InlineData("1.0", "http://example.com/path with spaces", "<http://example.com/path%20with%20spaces>")]
    [InlineData("1.1", "http://example.com/path with spaces", "http://example.com/path%20with%20spaces")]
    public void CreateTargetUriHeader(
        string version,
        string uri,
        string expectedUri)
    {
        var expectedText = $"WARC-Target-URI: {expectedUri}{WarcParser.CrLf}";

        var actualText = Utils.CreateTargetUriHeader(version, new Uri(uri));

        Assert.Equal(expectedText, actualText);
    }
}