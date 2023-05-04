namespace Toimik.WarcProtocol.Tests;

using System;
using System.Text;
using Xunit;

public class ConversionRecordTest
{
    [Fact]
    public void CreateWithCustomPayloadTypeIdentifierAndRecordBlockThatIsThePayload()
    {
        var record = new ConversionRecord(
            DateTime.Now,
            new SingleCrlfPayloadTypeIdentifier(),
            recordBlock: Encoding.UTF8.GetBytes("foobar"),
            contentType: "text/plain",
            infoId: new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"),
            targetUri: new Uri("file://var/www/htdoc/robots.txt"));

        Assert.Equal(SingleCrlfPayloadTypeIdentifier.PayloadType, record.IdentifiedPayloadType);
    }

    [Fact]
    public void CreateWithRecordBlockThatIsThePayload()
    {
        var record = new ConversionRecord(
            DateTime.Now,
            new PayloadTypeIdentifier(),
            recordBlock: Encoding.UTF8.GetBytes("foo"),
            contentType: "text/plain",
            infoId: Utils.CreateId(),
            targetUri: new Uri("dns://example.com"));

        Assert.Null(record.PayloadDigest);
        Assert.Null(record.IdentifiedPayloadType);
    }

    [Fact]
    public void WithContinuation()
    {
        var now = DateTime.Now;
        var payloadTypeIdentifier = new PayloadTypeIdentifier();
        var digestFactory = new DigestFactory("sha1");
        var payloadDigest = Utils.CreateWarcDigest(digestFactory, Encoding.UTF8.GetBytes("foobar"));
        var infoId = Utils.CreateId();
        var targetUri = new Uri("http://www.example.com");
        const string ContentType = "text/plain";
        var recordBlock = "foo";

        var conversionRecord = new ConversionRecord(
            now,
            payloadTypeIdentifier,
            recordBlock: Encoding.UTF8.GetBytes(recordBlock),
            ContentType,
            infoId: infoId,
            targetUri: targetUri,
            payloadDigest: payloadDigest);

        Assert.Equal("1.1", conversionRecord.Version);
        Assert.NotNull(conversionRecord.Id);
        Assert.Equal(payloadTypeIdentifier, conversionRecord.PayloadTypeIdentifier);
        var actualRecordBlock = Encoding.UTF8.GetString(conversionRecord.RecordBlock!);
        Assert.Equal(recordBlock, actualRecordBlock);
        Assert.Equal(ContentType, conversionRecord.ContentType);

        recordBlock = "bar";
        var continuationRecord = new ContinuationRecord(
            conversionRecord.Date,
            recordBlock: Encoding.UTF8.GetBytes(recordBlock),
            conversionRecord.PayloadDigest!,
            conversionRecord.InfoId!,
            conversionRecord.TargetUri!,
            conversionRecord.InfoId!,
            segmentNumber: 2);

        Assert.Equal("1.1", continuationRecord.Version);
        Assert.NotNull(continuationRecord.Id);
        Assert.Equal(now, continuationRecord.Date);
        actualRecordBlock = Encoding.UTF8.GetString(continuationRecord.RecordBlock!);
        Assert.Equal(recordBlock, actualRecordBlock);
        Assert.Equal(payloadDigest, continuationRecord.PayloadDigest);
        Assert.Equal(targetUri, continuationRecord.TargetUri);
        Assert.Equal(infoId, continuationRecord.InfoId);
        Assert.Equal(2, continuationRecord.SegmentNumber);
    }
}