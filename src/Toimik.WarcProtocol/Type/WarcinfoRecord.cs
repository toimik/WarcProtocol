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
    using System.Text;

    public class WarcinfoRecord : Record
    {
        public const string FieldForContentType = "content-type";

        public const string FieldForFilename = "warc-filename";

        internal static readonly IEnumerable<string> DefaultOrderedFields = new List<string>
        {
            FieldForType,
            FieldForRecordId,
            FieldForDate,
            FieldForContentLength,
            FieldForContentType,
            FieldForBlockDigest,
            FieldForTruncated,
            FieldForFilename,
        };

        public WarcinfoRecord(
            DateTime date,
            string contentBlock,
            string contentType,
            string filename = null,
            string truncatedReason = null,
            DigestFactory digestFactory = null)
            : this(
                  "1.1",
                  Utils.CreateId(),
                  date,
                  contentBlock,
                  contentType,
                  filename,
                  truncatedReason,
                  digestFactory)
        {
        }

        public WarcinfoRecord(
           string version,
           Uri recordId,
           DateTime date,
           string contentBlock,
           string contentType,
           string filename = null,
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
            ContentBlock = contentBlock;
            var bytes = Encoding.UTF8.GetBytes(ContentBlock);
            var isParsed = false;
            SetContentBlock(bytes, isParsed);

            if (contentBlock.Length > 0)
            {
                ContentType = contentType;
            }

            Filename = filename;
        }

        internal WarcinfoRecord(
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

        /// <remarks>
        /// May contain technical information such as base encoding of the digests used in named
        /// fields.
        /// </remarks>
        public string ContentBlock { get; private set; }

        public string ContentType { get; private set; }

        public string Filename { get; private set; }

        public override string Type => "Warcinfo";

        internal override void Set(string field, string value)
        {
            switch (field.ToLower())
            {
                case FieldForContentType:
                    ContentType = value;
                    break;

                case FieldForFilename:
                    Filename = value;
                    break;

                default:
                    base.Set(field, value);
                    break;
            }
        }

        internal override void SetContentBlock(byte[] contentBlock, bool isParsed = true)
        {
            base.SetContentBlock(contentBlock, isParsed);
            if (isParsed)
            {
                ContentBlock = Encoding.UTF8.GetString(contentBlock);
            }
        }

        protected override string GetHeader(string orderedField)
        {
            string text = null;
            switch (orderedField.ToLower())
            {
                case FieldForBlockDigest:
                    text = ToString("WARC-Block-Digest", BlockDigest);
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

                case FieldForFilename:
                    text = ToString("WARC-Filename", Filename);
                    break;

                case FieldForRecordId:
                    text = $"WARC-Record-ID: {Utils.AddBracketsToUri(Id)}{WarcParser.CrLf}";
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