namespace Toimik.WarcProtocol.Tests
{
    using System;
    using Xunit;

    public class WarcinfoRecordTest
    {
        [Fact]
        public void InstantiateUsingConstructorWithFewerParameters()
        {
            var now = DateTime.Now;
            var contentBlock = "...";
            var contentType = "application/warc-fields";
            var filename = "filename.warc";
            var record = new WarcinfoRecord(
                now,
                contentBlock,
                contentType,
                filename);

            Assert.Equal("1.1", record.Version);
            Assert.NotNull(record.Id);
            Assert.Equal(now, record.Date);
            Assert.Equal(contentBlock, record.ContentBlock);
            Assert.Equal(contentType, record.ContentType);
            Assert.Equal(filename, record.Filename);
        }
    }
}