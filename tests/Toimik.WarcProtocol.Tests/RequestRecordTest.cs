namespace Toimik.WarcProtocol.Tests;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class RequestRecordTest
{
    [Fact]
    public void CreateWithCustomPayloadTypeIdentifierAndContentBlockThatHasNoPayload()
    {
        const string ExpectedRecordBlock = "gemini://gemi.dev/why-gemini.gmi";

        var record = new RequestRecord(
            DateTime.Now,
            new SingleCrlfPayloadTypeIdentifier(),
            contentBlock: Encoding.UTF8.GetBytes(ExpectedRecordBlock),
            contentType: "application/gemini; msgtype=request",
            infoId: new Uri("urn:uuid:1d0cf87c-b70a-4df6-9ff8-dd599494058d"),
            targetUri: new Uri("gemini://gemi.dev/why-gemini.gmi"));

        Assert.Equal(ExpectedRecordBlock, record.RecordBlock);
        Assert.Null(record.Payload);
        Assert.Null(record.IdentifiedPayloadType);
    }

    [Fact]
    public void CreateWithCustomPayloadTypeIdentifierAndContentBlockThatHasPayload()
    {
        const string ExpectedRecordBlock = "gemini://gemi.dev/why-gemini.gmi";
        var expectedPayload = $"# A test file that doesn't have a double CRLF anywhere.{WarcParser.CrLf}Just A Test";
        var contentBlock = $"{ExpectedRecordBlock}{SingleCrlfPayloadTypeIdentifier.CreateDelimiterText(SingleCrlfPayloadTypeIdentifier.GeminiDelimiter)}{expectedPayload}";

        var record = new RequestRecord(
            DateTime.Now,
            new SingleCrlfPayloadTypeIdentifier(),
            contentBlock: Encoding.UTF8.GetBytes(contentBlock),
            contentType: "application/gemini; msgtype=request",
            infoId: new Uri("urn:uuid:1d0cf87c-b70a-4df6-9ff8-dd599494058d"),
            targetUri: new Uri("gemini://gemi.dev/why-gemini.gmi"));

        Assert.Equal(ExpectedRecordBlock, record.RecordBlock);
        var actualPayload = Encoding.UTF8.GetString(record.Payload!);
        Assert.Equal(expectedPayload, actualPayload);
        Assert.Equal(SingleCrlfPayloadTypeIdentifier.PayloadType, record.IdentifiedPayloadType);
    }

    [Fact]
    public void CreateWithFewerParameters()
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
        var actualPayload = Encoding.UTF8.GetString(record.Payload!);
        Assert.Equal(Payload, actualPayload);
    }

    [Fact]
    public async Task ParseWithCustomPayloadTypeIdentifierAndContentBlockThatHasNoPayload()
    {
        const string ExpectedRecordBlock = "gemini://gemi.dev/why-gemini.gmi";
        var recordFactory = new RecordFactory(payloadTypeIdentifier: new SingleCrlfPayloadTypeIdentifier());
        var parser = new WarcParser(recordFactory);
        var path = $"{WarcParserTest.DirectoryForValidRecords}1.1{Path.DirectorySeparatorChar}misc{Path.DirectorySeparatorChar}request_gemini_wo_payload.warc";

        var records = await parser.Parse(path).ToListAsync();
        var actualRecord = (RequestRecord)records[0];

        Assert.Equal(ExpectedRecordBlock, actualRecord.RecordBlock);
        Assert.Null(actualRecord.Payload);
        Assert.Null(actualRecord.IdentifiedPayloadType);
    }

    [Fact]
    public async Task ParseWithCustomPayloadTypeIdentifierAndContentBlockThatHasPayload()
    {
        const string ExpectedRecordBlock = "gemini://gemi.dev/why-gemini.gmi";
        var expectedPayload = $"# A test file that doesn't have a double CRLF anywhere.{WarcParser.CrLf}Just A Test";
        var recordFactory = new RecordFactory(payloadTypeIdentifier: new SingleCrlfPayloadTypeIdentifier());
        var parser = new WarcParser(recordFactory);
        var path = $"{WarcParserTest.DirectoryForValidRecords}1.1{Path.DirectorySeparatorChar}misc{Path.DirectorySeparatorChar}request_gemini_w_payload.warc";

        var records = await parser.Parse(path).ToListAsync();
        var actualRecord = (RequestRecord)records[0];

        Assert.Equal(ExpectedRecordBlock, actualRecord.RecordBlock);
        var payload = actualRecord.Payload!;
        var actualPayload = Encoding.UTF8.GetString(payload);
        Assert.Equal(expectedPayload, actualPayload);
        var identifiedPayloadType = actualRecord.PayloadTypeIdentifier.Identify(payload);
        Assert.Equal(SingleCrlfPayloadTypeIdentifier.PayloadType, identifiedPayloadType);
        Assert.Null(actualRecord.IdentifiedPayloadType);
    }
}