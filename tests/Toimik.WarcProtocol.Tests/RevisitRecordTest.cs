namespace Toimik.WarcProtocol.Tests
{
    using System;
    using Xunit;

    public class RevisitRecordTest
    {
        [Fact]
        public void InstantiateUsingConstructorWithFewerParameters()
        {
            var now = DateTime.Now;
            var recordBlock = "foobar";
            var contentType = "message/http";
            var infoId = Utils.CreateId();
            var targetUri = new Uri("http://www.example.com");
            var profile = new Uri("http://netpreserve.org/warc/1.1/revisit/identical-payload-digest");
            var record = new RevisitRecord(
                now,
                recordBlock,
                contentType,
                infoId,
                targetUri,
                profile);

            Assert.Equal("1.1", record.Version);
            Assert.NotNull(record.Id);
            Assert.Equal(now, record.Date);
            Assert.Equal(recordBlock, record.RecordBlock);
            Assert.Equal(contentType, record.ContentType);
            Assert.Equal(infoId, record.InfoId);
            Assert.Equal(targetUri, record.TargetUri);
            Assert.Equal(profile, record.Profile);
        }
    }
}