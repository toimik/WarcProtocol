namespace Toimik.WarcProtocol.Tests;

using System;
using Xunit;

public class WarcinfoRecordTest
{
    [Fact]
    public void InstantiateUsingConstructorWithFewerParameters()
    {
        var now = DateTime.Now;
        const string ContentBlock = "...";
        const string ContentType = "application/warc-fields";
        const string Filename = "filename.warc";
        var record = new WarcinfoRecord(
            now,
            ContentBlock,
            ContentType,
            Filename);

        Assert.Equal("1.1", record.Version);
        Assert.NotNull(record.Id);
        Assert.Equal(now, record.Date);
        Assert.Equal(ContentBlock, record.ContentBlock);
        Assert.Equal(ContentType, record.ContentType);
        Assert.Equal(Filename, record.Filename);
    }
}