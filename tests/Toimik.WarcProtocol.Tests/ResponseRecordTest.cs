namespace Toimik.WarcProtocol.Tests
{
    using System;
    using System.Text;
    using Xunit;

    public class ResponseRecordTest
    {
        [Fact]
        public void InstantiateUsingConstructorWithFewerParameters()
        {
            var now = DateTime.Now;
            var payloadTypeIdentifier = new PayloadTypeIdentifier();
            var text = "HTTP/1.1 200 OK";
            var contentBlock = Encoding.UTF8.GetBytes(text);
            var digestFactory = new DigestFactory("sha1");
            var payloadDigest = Utils.CreateWarcDigest(digestFactory, contentBlock);
            var contentType = "application/http;msgtype=response";
            var infoId = Utils.CreateId();
            var targetUri = new Uri("http://www.example.com");
            var record = new ResponseRecord(
                now,
                payloadTypeIdentifier,
                contentBlock,
                contentType: contentType,
                infoId: infoId,
                targetUri: targetUri,
                payloadDigest: payloadDigest);

            Assert.Equal("1.1", record.Version);
            Assert.NotNull(record.Id);
            Assert.Equal(now, record.Date);
            Assert.Equal(payloadTypeIdentifier, record.PayloadTypeIdentifier);
            Assert.Equal(contentBlock, record.ContentBlock);
            Assert.Equal(payloadDigest, record.PayloadDigest);
            Assert.Equal(contentType, record.ContentType);
            Assert.Equal(infoId, record.InfoId);
            Assert.Equal(targetUri, record.TargetUri);
            Assert.Empty(record.Payload);
        }
    }
}