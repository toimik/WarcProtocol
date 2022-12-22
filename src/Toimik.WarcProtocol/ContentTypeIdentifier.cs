/*
 * Copyright 2021-2022 nurhafiz@hotmail.sg
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
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

namespace Toimik.WarcProtocol;

public class ContentTypeIdentifier
{
    public ContentTypeIdentifier()
    {
    }

    public virtual string Identify(Record record)
    {
        string? contentType = null;
        var recordType = record.Type;
        switch (recordType)
        {
            case ResourceRecord.TypeName:
                var targetUri = ((ResourceRecord)record).TargetUri;
                if (targetUri != null
                    && targetUri.Scheme.Equals("dns"))
                {
                    contentType = "text/dns";
                }

                break;

            case RequestRecord.TypeName:
            case ResponseRecord.TypeName:
                targetUri = record is RequestRecord requestRecord
                    ? requestRecord.TargetUri
                    : ((ResponseRecord)record).TargetUri;
                if (targetUri != null
                    && targetUri.Scheme.StartsWith("http"))
                {
                    contentType = $"application/http;msgtype={recordType}";
                }

                break;

            case MetadataRecord.TypeName:
            case WarcinfoRecord.TypeName:
                contentType = "application/warc-fields";
                break;
        }

        contentType ??= "application/octet-stream";
        return contentType;
    }
}