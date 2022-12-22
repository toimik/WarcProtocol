namespace Toimik.WarcProtocol.Tests;

using System;
using Xunit;

public class MetadataRecordTest
{
    [Fact]
    public void InstantiateUsingConstructorWithFewerParameters()
    {
        var now = DateTime.Now;
        var contentBlock = "foobar";
        var contentType = "application/warc-fields";
        var infoId = Utils.CreateId();

        var record = new MetadataRecord(
            now,
            contentBlock,
            contentType,
            infoId);

        Assert.Equal("1.1", record.Version);
        Assert.NotNull(record.Id);
        Assert.Equal(now, record.Date);
        Assert.Equal(contentBlock, record.ContentBlock);
        Assert.Equal(contentType, record.ContentType);
        Assert.Equal(infoId, record.InfoId);
    }
}