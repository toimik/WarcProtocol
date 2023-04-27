namespace Toimik.WarcProtocol.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class ResponseRecordTest
{
    [Fact]
    public void InstantiateUsingConstructorWithFewerParameters()
    {
        var now = DateTime.Now;
        var payloadTypeIdentifier = new PayloadTypeIdentifier();
        var contentBlock = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK");
        var digestFactory = new DigestFactory("sha1");
        var payloadDigest = Utils.CreateWarcDigest(digestFactory, contentBlock);
        const string ContentType = "application/http;msgtype=response";
        var infoId = Utils.CreateId();
        var targetUri = new Uri("http://www.example.com");
        var record = new ResponseRecord(
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
        Assert.Null(record.Payload);
    }

    [Fact]
    public async Task ParseWithPayloadThatDoesNotExist()
    {
        const string ExpectedRecordBlock = "20 text/gemini";
        var recordFactory = new RecordFactory(payloadTypeIdentifier: new SingleCrlfPayloadTypeIdentifier());
        var parser = new WarcParser(recordFactory);
        var path = $"{WarcParserTest.DirectoryForValidRecords}1.1{Path.DirectorySeparatorChar}misc{Path.DirectorySeparatorChar}response_gemini_wo_payload.warc";

        var records = await parser.Parse(path).ToListAsync().ConfigureAwait(false);
        var actualRecord = (ResponseRecord)records[0];

        Assert.Equal(ExpectedRecordBlock, actualRecord.RecordBlock);
        Assert.Null(actualRecord.Payload);
        Assert.Null(actualRecord.IdentifiedPayloadType);
    }

    [Fact]
    public async Task ParseWithPayloadThatExistButDelimitedByCustomDelimiter()
    {
        const string ExpectedRecordBlock = "20 text/gemini";
        var expectedPayload = $"# A test file that doesn't have a double CRLF anywhere.{WarcParser.CrLf}Just A Test";
        var recordFactory = new RecordFactory(payloadTypeIdentifier: new SingleCrlfPayloadTypeIdentifier());
        var parser = new WarcParser(recordFactory);
        var path = $"{WarcParserTest.DirectoryForValidRecords}1.1{Path.DirectorySeparatorChar}misc{Path.DirectorySeparatorChar}response_gemini_w_payload.warc";

        var records = await parser.Parse(path).ToListAsync().ConfigureAwait(false);
        var actualRecord = (ResponseRecord)records[0];

        Assert.Equal(ExpectedRecordBlock, actualRecord.RecordBlock);
        var payload = actualRecord.Payload!;
        var actualPayload = Encoding.UTF8.GetString(payload);
        Assert.Equal(expectedPayload, actualPayload);
        Assert.Equal(SingleCrlfPayloadTypeIdentifier.PayloadType, actualRecord.IdentifiedPayloadType);
        Assert.Equal(SingleCrlfPayloadTypeIdentifier.PayloadType, actualRecord.PayloadTypeIdentifier.Identify(payload));
    }

    private class SingleCrlfPayloadTypeIdentifier : PayloadTypeIdentifier
    {
        public const string PayloadType = "foobar";

        private static readonly int[] GeminiDelimiter = new int[]
        {
            WarcParser.CarriageReturn,
            WarcParser.LineFeed,
        };

        public SingleCrlfPayloadTypeIdentifier()
            : base(GeminiDelimiter)
        {
        }

        public override string? Identify(byte[] payload)
        {
            var type = payload.Length == 0
                ? null
                : PayloadType;
            return type;
        }
    }
}