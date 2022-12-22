namespace Toimik.WarcProtocol.Tests;

using System;
using System.Text;
using Xunit;

public class RequestRecordTest
{
    [Fact]
    public void InstantiateUsingConstructorWithFewerParameters()
    {
        var now = DateTime.Now;
        var payloadTypeIdentifier = new PayloadTypeIdentifier();
        const string Payload = "payload";
        var text = $"GET / HTTP/1.1{WarcParser.CrLf}Host: www.example.com{WarcParser.CrLf}Content-Length: 7{WarcParser.CrLf}{WarcParser.CrLf}{Payload}";
        var contentBlock = Encoding.UTF8.GetBytes(text);
        var digestFactory = new DigestFactory("sha1");
        var payloadDigest = Utils.CreateWarcDigest(digestFactory, contentBlock);
        const string ContentType = "application/http;msgtype=request";
        var infoId = Utils.CreateId();
        var targetUri = new Uri("http://www.example.com");
        var record = new RequestRecord(
            now,
            payloadTypeIdentifier,
            contentBlock,
            ContentType,
            infoId: infoId,
            targetUri: targetUri,
            payloadDigest: payloadDigest);

        Assert.Equal("1.1", record.Version);
        Assert.NotNull(record.Id);
        Assert.Equal(now, record.Date);
        Assert.Equal(payloadTypeIdentifier, record.PayloadTypeIdentifier);
        Assert.Equal(contentBlock, record.ContentBlock);
        Assert.Equal(payloadDigest, record.PayloadDigest);
        Assert.Equal(ContentType, record.ContentType);
        Assert.Equal(infoId, record.InfoId);
        Assert.Equal(targetUri, record.TargetUri);
        Assert.Equal(Payload, Encoding.UTF8.GetString(record.Payload!));
    }
}