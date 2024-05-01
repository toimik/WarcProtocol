/*
 * Copyright 2021-2024 nurhafiz@hotmail.sg
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

using System;

public class RecordFactory(DigestFactory? digestFactory = null, PayloadTypeIdentifier? payloadTypeIdentifier = null)
{
    public DigestFactory? DigestFactory { get; } = digestFactory;

    public PayloadTypeIdentifier PayloadTypeIdentifier { get; } = payloadTypeIdentifier ?? new PayloadTypeIdentifier();

    public virtual Record CreateRecord(
        string version,
        string recordType,
        Uri recordId,
        DateTime date)
    {
        recordType = recordType.ToLower();
        Record? record = recordType switch
        {
            ContinuationRecord.TypeName => new ContinuationRecord(
                version,
                recordId,
                date,
                DigestFactory),
            ConversionRecord.TypeName => new ConversionRecord(
                version,
                recordId,
                date,
                DigestFactory,
                PayloadTypeIdentifier),
            MetadataRecord.TypeName => new MetadataRecord(
                version,
                recordId,
                date,
                DigestFactory),
            RequestRecord.TypeName => new RequestRecord(
                version,
                recordId,
                date,
                DigestFactory,
                PayloadTypeIdentifier),
            ResourceRecord.TypeName => new ResourceRecord(
                version,
                recordId,
                date,
                DigestFactory,
                PayloadTypeIdentifier),
            ResponseRecord.TypeName => new ResponseRecord(
                version,
                recordId,
                date,
                DigestFactory,
                PayloadTypeIdentifier),
            RevisitRecord.TypeName => new RevisitRecord(
                version,
                recordId,
                date,
                DigestFactory),
            WarcinfoRecord.TypeName => new WarcinfoRecord(
                version,
                recordId,
                date,
                DigestFactory),
            _ => throw new ArgumentException($"Unsupported record type: {recordType}"),
        };

        return record;
    }
}