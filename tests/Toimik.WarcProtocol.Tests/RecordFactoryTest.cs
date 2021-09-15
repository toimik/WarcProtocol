namespace Toimik.WarcProtocol.Tests
{
    using System;
    using Xunit;

    public class RecordFactoryTest

    {
        [Fact]
        public void InvalidRecordType()
        {
            var factory = new RecordFactory();
            var record = factory.CreateRecord(
                version: "1.1",
                recordType: "invalid",
                recordId: Utils.CreateId(),
                date: DateTime.Now);

            Assert.Null(record);
        }
    }
}