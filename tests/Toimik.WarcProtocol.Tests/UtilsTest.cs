namespace Toimik.WarcProtocol.Tests;

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

public class UtilsTest
{
    public static IEnumerable<object[]> PayloadData => new List<object[]>
    {
        new object[] { -1, $"foobar", },
        new object[] { 3, $"foo{WarcParser.CrLf}{WarcParser.CrLf}bar", },
        new object[] { 6, $"foobar{WarcParser.CrLf}{WarcParser.CrLf}fuzz", },
    };

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

    [Theory]
    [MemberData(nameof(PayloadData))]
    public void IndexOfPayload(int expectedIndex, string contentBlock)
    {
        var actualIndex = Utils.IndexOfPayload(Encoding.UTF8.GetBytes(contentBlock));

        Assert.Equal(expectedIndex, actualIndex);
    }

    [Theory]
    [InlineData("urn:uuid:d1905d86-66a1-4910-8c69-81ec8f9c2c95", "urn:uuid:d1905d86-66a1-4910-8c69-81ec8f9c2c95")]
    [InlineData("<urn:uuid:d1905d86-66a1-4910-8c69-81ec8f9c2c95>", "urn:uuid:d1905d86-66a1-4910-8c69-81ec8f9c2c95")]
    public void RemoveBracketsFromUri(string uri, string expectedUri)
    {
        var actualUri = Utils.RemoveBracketsFromUri(uri);

        Assert.Equal(expectedUri, actualUri.ToString());
    }
}