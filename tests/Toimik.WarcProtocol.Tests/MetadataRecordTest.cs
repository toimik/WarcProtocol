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
}