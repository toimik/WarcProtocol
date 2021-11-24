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
    using System.Net;
    using System.Text;

    public class ResponseRecord : Record
    {
        public const string FieldForConcurrentTo = "warc-concurrent-to";

        public const string FieldForContentType = "content-type";

        public const string FieldForIdentifiedPayloadType = "warc-identified-payload-type";

        public const string FieldForInfoId = "warc-warcinfo-id";

        public const string FieldForIpAddress = "warc-ip-address";

        public const string FieldForPayloadDigest = "warc-payload-digest";

        public const string FieldForSegmentNumber = "warc-segment-number";

        public const string FieldForTargetUri = "warc-target-uri";

        internal static readonly IEnumerable<string> DefaultOrderedFields = new List<string>
        {
            FieldForType,
            FieldForRecordId,
            FieldForDate,
            FieldForContentLength,
            FieldForContentType,
            FieldForConcurrentTo,
            FieldForBlockDigest,
            FieldForPayloadDigest,
            FieldForIpAddress,
            FieldForTargetUri,
            FieldForTruncated,
            FieldForInfoId,
            FieldForIdentifiedPayloadType,
            FieldForSegmentNumber,
        };

        public ResponseRecord(
            DateTime date,
            PayloadTypeIdentifier payloadTypeIdentifier,
            byte[] contentBlock,
            string contentType,
            Uri infoId,
            Uri targetUri,
            string payloadDigest = null,
            IPAddress ipAddress = null,
            ISet<Uri> concurrentTos = null,
            bool isSegmented = false,
            string truncatedReason = null,
            DigestFactory digestFactory = null)
            : this(
                  "1.1",
                  Utils.CreateId(),
                  date,
                  payloadTypeIdentifier,
                  contentBlock,
                  contentType,
                  infoId,
                  targetUri,
                  payloadDigest,
                  ipAddress,
                  concurrentTos,
                  isSegmented,
                  truncatedReason,
                  digestFactory)
        {
        }

        public ResponseRecord(
            string version,
            Uri recordId,
            DateTime date,
            PayloadTypeIdentifier payloadTypeIdentifier,
            byte[] contentBlock,
            string contentType,
            Uri infoId,
            Uri targetUri,
            string payloadDigest = null,
            IPAddress ipAddress = null,
            ISet<Uri> concurrentTos = null,
            bool isSegmented = false,
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
            PayloadTypeIdentifier = payloadTypeIdentifier;

            var isParsed = false;
            SetContentBlock(contentBlock, isParsed);

            PayloadDigest = payloadDigest ?? Utils.CreateWarcDigest(DigestFactory, Payload);
            if (contentBlock.Length > 0)
            {
                ContentType = contentType;
            }

            InfoId = infoId;
            TargetUri = targetUri;
            IpAddress = ipAddress;
            ConcurrentTos = concurrentTos;
            SegmentNumber = isSegmented
               ? 1
               : null;
        }

        internal ResponseRecord(
            string version,
            Uri recordId,
            DateTime date,
            DigestFactory digestFactory,
            PayloadTypeIdentifier payloadTypeIdentifier)
            : base(
                  version,
                  recordId,
                  date,
                  DefaultOrderedFields,
                  digestFactory: digestFactory)
        {
            PayloadTypeIdentifier = payloadTypeIdentifier;
        }

        public ISet<Uri> ConcurrentTos { get; } = new HashSet<Uri>();

        public byte[] ContentBlock { get; private set; }

        public string ContentType { get; private set; }

        public string IdentifiedPayloadType { get; private set; }

        public Uri InfoId { get; private set; }

        public IPAddress IpAddress { get; private set; }

        public byte[] Payload { get; private set; }

        public string PayloadDigest { get; private set; }

        public PayloadTypeIdentifier PayloadTypeIdentifier { get; }

        public string RecordBlock { get; private set; }

        public int? SegmentNumber { get; private set; }

        public Uri TargetUri { get; private set; }

        public override string Type => "Response";

        public bool IsSegmented() => SegmentNumber != 0;

        internal override void Set(string field, string value)
        {
            // NOTE: FieldForIdentifiedPayloadType, if any, is ignored because it is supposed to be
            // auto detected when the content block is set
            switch (field.ToLower())
            {
                case FieldForConcurrentTo:
                    ConcurrentTos.Add(Utils.RemoveBracketsFromUri(value));
                    break;

                case FieldForContentType:
                    ContentType = value;
                    break;

                case FieldForInfoId:
                    InfoId = Utils.RemoveBracketsFromUri(value);
                    break;

                case FieldForIpAddress:
                    IpAddress = IPAddress.Parse(value);
                    break;

                case FieldForPayloadDigest:
                    PayloadDigest = value;
                    break;

                case FieldForSegmentNumber:
                    SegmentNumber = int.Parse(value);
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
            var index = Utils.IndexOfPayload(contentBlock);
            if (index == -1)
            {
                RecordBlock = Encoding.UTF8.GetString(contentBlock);
                Payload = Array.Empty<byte>();
            }
            else
            {
                RecordBlock = Encoding.UTF8.GetString(contentBlock[0..index]);
                Payload = contentBlock[(index + (WarcParser.CrLf.Length * 2))..];
            }

            ContentBlock = contentBlock;
            IdentifiedPayloadType = PayloadTypeIdentifier.Identify(Payload);
        }

        protected override string GetHeader(string field)
        {
            string text = null;
            switch (field.ToLower())
            {
                case FieldForBlockDigest:
                    text = ToString("WARC-Block-Digest", BlockDigest);
                    break;

                case FieldForConcurrentTo:
                    foreach (Uri concurrentTo in ConcurrentTos)
                    {
                        text = $"WARC-Concurrent-To: {Utils.AddBracketsToUri(concurrentTo)}{WarcParser.CrLf}";
                    }

                    break;

                case FieldForContentLength:
                    text = $"Content-Length: {ContentLength}{WarcParser.CrLf}";
                    break;

                case FieldForContentType:
                    text = ToString("Content-Type", ContentType);
                    break;

                case FieldForDate:
                    text = $"WARC-Date: {Utils.FormatDate(Date)}{WarcParser.CrLf}";
                    break;

                case FieldForIdentifiedPayloadType:
                    text = ToString("WARC-Identified-Payload-Type", IdentifiedPayloadType);
                    break;

                case FieldForInfoId:
                    text = ToString("WARC-Warcinfo-ID", Utils.AddBracketsToUri(InfoId));
                    break;

                case FieldForIpAddress:
                    text = ToString("WARC-IP-Address", IpAddress);
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