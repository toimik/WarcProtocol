namespace Toimik.WarcProtocol.Tests;

using System;
using System.Text;
using Xunit;

public class ResourceRecordTest
{
    [Fact]
    public void CreateWithCustomPayloadTypeIdentifierAndRecordBlockThatIsThePayload()
    {
        var record = new ResourceRecord(
            DateTime.Now,
            new SingleCrlfPayloadTypeIdentifier(),
            recordBlock: Encoding.UTF8.GetBytes("foo"),
            contentType: "text/plain",
            infoId: Utils.CreateId(),
            targetUri: new Uri("dns://example.com"));

        Assert.Equal(SingleCrlfPayloadTypeIdentifier.PayloadType, record.IdentifiedPayloadType);
    }

    [Fact]
    public void CreateWithRecordBlockThatIsThePayload()
    {
        var record = new ResourceRecord(
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
        const string ContentType = "text/plain";
        var infoId = Utils.CreateId();
        var targetUri = new Uri("http://www.example.com");
        var recordBlock = "foo";

        var resourceRecord = new ResourceRecord(
            now,
            payloadTypeIdentifier,
            recordBlock: Encoding.UTF8.GetBytes(recordBlock),
            ContentType,
            infoId: infoId,
            targetUri: targetUri,
            payloadDigest: payloadDigest);

        Assert.Equal("1.1", resourceRecord.Version);
        Assert.NotNull(resourceRecord.Id);
        Assert.Equal(payloadTypeIdentifier, resourceRecord.PayloadTypeIdentifier);
        var actualRecordBlock = Encoding.UTF8.GetString(resourceRecord.RecordBlock!);
        Assert.Equal(recordBlock, actualRecordBlock);
        Assert.Equal(ContentType, resourceRecord.ContentType);

        recordBlock = "bar";
        var continuationRecord = new ContinuationRecord(
            resourceRecord.Date,
            recordBlock: Encoding.UTF8.GetBytes(recordBlock),
            resourceRecord.PayloadDigest!,
            resourceRecord.InfoId!,
            resourceRecord.TargetUri!,
            resourceRecord.InfoId!,
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