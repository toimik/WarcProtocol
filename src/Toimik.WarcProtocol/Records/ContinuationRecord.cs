/*
 * Copyright 2021 nurhafiz@hotmail.sg
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

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Toimik.WarcProtocol.Tests")]

namespace Toimik.WarcProtocol
{
    using System;
    using System.Collections.Generic;

    public class ContinuationRecord : Record
    {
        public const string FieldForInfoId = "warc-warcinfo-id";

        public const string FieldForPayloadDigest = "warc-payload-digest";

        public const string FieldForSegmentNumber = "warc-segment-number";

        public const string FieldForSegmentOriginId = "warc-segment-origin-id";

        public const string FieldForSegmentTotalLength = "warc-segment-total-length";

        public const string FieldForTargetUri = "warc-target-uri";

        internal static readonly IEnumerable<string> DefaultOrderedFields = new List<string>
        {
            FieldForType,
            FieldForRecordId,
            FieldForDate,
            FieldForContentLength,
            FieldForBlockDigest,
            FieldForPayloadDigest,
            FieldForTargetUri,
            FieldForTruncated,
            FieldForInfoId,
            FieldForSegmentOriginId,
            FieldForSegmentNumber,
            FieldForSegmentTotalLength,
        };

        public ContinuationRecord(
            DateTime date,
            byte[] recordBlock,
            string payloadDigest,
            Uri infoId,
            Uri targetUri,
            Uri segmentOriginId,
            int segmentNumber,
            int? segmentTotalLength = null,
            string truncatedReason = null,
            DigestFactory digestFactory = null)
            : this(
                  "1.1",
                  Utils.CreateId(),
                  date,
                  recordBlock,
                  payloadDigest,
                  infoId,
                  targetUri,
                  segmentOriginId,
                  segmentNumber,
                  segmentTotalLength,
                  truncatedReason,
                  digestFactory)
        {
        }

        public ContinuationRecord(
            string version,
            Uri recordId,
            DateTime date,
            byte[] recordBlock,
            string payloadDigest,
            Uri infoId,
            Uri targetUri,
            Uri segmentOriginId,
            int segmentNumber,
            int? segmentTotalLength = null,
            string truncatedReason = null,
            DigestFactory digestFactory = null)
            : base(
                  version,
                  recordId,
                  date,
                  DefaultOrderedFields,
                  truncatedReason,
                  digestFactory)
        {
            var isParsed = false;
            SetContentBlock(recordBlock, isParsed);

            // REMINDER: This is not auto-generated because it must be identical to the source's
            PayloadDigest = payloadDigest;
            InfoId = infoId;
            TargetUri = targetUri;
            SegmentOriginId = segmentOriginId;
            SegmentNumber = segmentNumber;
            SegmentTotalLength = segmentTotalLength;
        }

        internal ContinuationRecord(
            string version,
            Uri recordId,
            DateTime date,
            DigestFactory digestFactory)
            : base(
                  version,
                  recordId,
                  date,
                  DefaultOrderedFields,
                  digestFactory: digestFactory)
        {
        }

        public Uri InfoId { get; private set; }

        public string PayloadDigest { get; private set; }

        public byte[] RecordBlock { get; private set; }

        public int SegmentNumber { get; private set; }

        public Uri SegmentOriginId { get; private set; }

        // NOTE: Applicable to the last continuation in the series only
        public int? SegmentTotalLength { get; private set; }

        public Uri TargetUri { get; private set; }

        public override string Type => "Continuation";

        internal override void Set(string field, string value)
        {
            switch (field.ToLower())
            {
                case FieldForInfoId:
                    InfoId = Utils.RemoveBracketsFromUri(value);
                    break;

                case FieldForPayloadDigest:
                    PayloadDigest = value;
                    break;

                case FieldForSegmentOriginId:
                    SegmentOriginId = Utils.RemoveBracketsFromUri(value);
                    break;

                case FieldForSegmentNumber:
                    SegmentNumber = int.Parse(value);
                    break;

                case FieldForSegmentTotalLength:
                    SegmentTotalLength = int.Parse(value);
                    break;

                case FieldForTargetUri:
                    TargetUri = Version.Equals("1.0")
                        ? Utils.RemoveBracketsFromUri(value)
                        : new(value);
                    break;

                default:
                    base.Set(field, value);
                    break;
            }
        }

        internal override void SetContentBlock(byte[] contentBlock, bool isParsed = true)
        {
            base.SetContentBlock(contentBlock, isParsed);
            RecordBlock = contentBlock;
        }

        protected override string GetHeader(string field)
        {
            string text = null;
            switch (field.ToLower())
            {
                case FieldForBlockDigest:
                    text = ToString("WARC-Block-Digest", BlockDigest);
                    break;

                case FieldForContentLength:
                    text = $"Content-Length: {ContentLength}{WarcParser.CrLf}";
                    break;

                case FieldForDate:
                    text = $"WARC-Date: {Utils.FormatDate(Date)}{WarcParser.CrLf}";
                    break;

                case FieldForInfoId:
                    text = ToString("WARC-Warcinfo-ID", Utils.AddBracketsToUri(InfoId));
                    break;

                case FieldForPayloadDigest:
                    text = ToString("WARC-Payload-Digest", PayloadDigest);
                    break;

                case FieldForRecordId:
                    text = $"WARC-Record-ID: {Utils.AddBracketsToUri(Id)}{WarcParser.CrLf}";
                    break;

                case FieldForSegmentNumber:
                    text = ToString("WARC-Segment-Number", SegmentNumber);
                    break;

                case FieldForSegmentOriginId:
                    text = ToString("WARC-Segment-Origin-ID", Utils.AddBracketsToUri(SegmentOriginId));
                    break;

                case FieldForSegmentTotalLength:
                    text = ToString("WARC-Segment-Total-Length", SegmentTotalLength);
                    break;

                case FieldForTargetUri:
                    var targetUri = Version.Equals("1.0")
                        ? Utils.AddBracketsToUri(TargetUri)
                        : TargetUri.ToString();
                    text = ToString("WARC-Target-URI", targetUri);
                    break;

                case FieldForTruncated:
                    text = ToString("WARC-Truncated", TruncatedReason);
                    break;

                case FieldForType:
                    text = $"WARC-Type: {Type.ToString().ToLower()}{WarcParser.CrLf}";
                    break;
            }

            return text;
        }
    }
}