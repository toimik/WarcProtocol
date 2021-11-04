/*
 * Copyright 2021 nurhafiz@hotmail.sg
 *
 * Licensed under the Apache License, version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Toimik.WarcProtocol.Tests
{
    using System;
    using System.Text;
    using Xunit;

    public class ResourceRecordTest
    {
        [Fact]
        public void WithContinuation()
        {
            var now = DateTime.Now;
            var payloadTypeIdentifier = new PayloadTypeIdentifier();
            var digestFactory = new DigestFactory("sha1");
            var payloadDigest = Utils.CreateWarcDigest(digestFactory, Encoding.UTF8.GetBytes("foobar"));
            var infoId = Utils.CreateId();
            var targetUri = new Uri("http://www.example.com");

            var resourceRecord = new ResourceRecord(
                now,
                payloadTypeIdentifier,
                recordBlock: Encoding.UTF8.GetBytes("foo"),
                contentType: "text/plain",
                infoId: infoId,
                targetUri: targetUri,
                payloadDigest: payloadDigest);

            Assert.Equal("1.1", resourceRecord.Version);
            Assert.NotNull(resourceRecord.Id);
            Assert.Equal(payloadTypeIdentifier, resourceRecord.PayloadTypeIdentifier);
            Assert.Equal("foo", Encoding.UTF8.GetString(resourceRecord.RecordBlock));
            Assert.Equal("text/plain", resourceRecord.ContentType);

            var continuationRecord = new ContinuationRecord(
                resourceRecord.Date,
                recordBlock: Encoding.UTF8.GetBytes("bar"),
                resourceRecord.PayloadDigest,
                resourceRecord.InfoId,
                resourceRecord.TargetUri,
                resourceRecord.InfoId,
                segmentNumber: 2);

            Assert.Equal("1.1", continuationRecord.Version);
            Assert.NotNull(continuationRecord.Id);
            Assert.Equal(now, continuationRecord.Date);
            Assert.Equal("bar", Encoding.UTF8.GetString(continuationRecord.RecordBlock));
            Assert.Equal(payloadDigest, continuationRecord.PayloadDigest);
            Assert.Equal(targetUri, continuationRecord.TargetUri);
            Assert.Equal(infoId, continuationRecord.InfoId);
            Assert.Equal(2, continuationRecord.SegmentNumber);
        }

        [Fact]
        public void WithoutPayloadDigest()
        {
            var resourceRecord = new ResourceRecord(
                DateTime.Now,
                new PayloadTypeIdentifier(),
                Encoding.UTF8.GetBytes("foo"),
                contentType: "text/plain",
                infoId: Utils.CreateId(),
                targetUri: new Uri("dns://example.com"));

            var digestFactory = new DigestFactory("sha1");
            var expectedPayloadDigest = Utils.CreateWarcDigest(digestFactory, Encoding.UTF8.GetBytes("foo"));
            Assert.Equal(expectedPayloadDigest, resourceRecord.PayloadDigest);
        }
    }
}