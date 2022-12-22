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

using System;
using System.Collections.Generic;
using System.Text;

public abstract class Record
{
    public const string FieldForBlockDigest = "warc-block-digest";

    public const string FieldForContentLength = "content-length";

    public const string FieldForDate = "warc-date";

    public const string FieldForRecordId = "warc-record-id";

    public const string FieldForTruncated = "warc-truncated";

    public const string FieldForType = "warc-type";

    protected Record(
        string version,
        Uri recordId,
        DateTime date,
        IEnumerable<string> orderedFields,
        string? truncatedReason = null,
        DigestFactory? digestFactory = null)
    {
        Version = version.Trim();
        Id = recordId;
        Date = date;
        OrderedFields = orderedFields;
        TruncatedReason = truncatedReason;
        DigestFactory = digestFactory ?? new DigestFactory("sha1");
    }

    public string? BlockDigest { get; private set; }

    public int ContentLength { get; private set; }

    public DateTime Date { get; private set; }

    public DigestFactory DigestFactory { get; }

    public Uri Id { get; private set; }

    public IEnumerable<string> OrderedFields { get; }

    public string? TruncatedReason { get; private set; }

    public abstract string Type { get; }

    public string Version { get; }

    public string GetHeader(IEnumerable<string>? orderedFields = null)
    {
        orderedFields ??= OrderedFields;
        var builder = new StringBuilder($"WARC/{Version}{WarcParser.CrLf}");
        foreach (string orderedField in orderedFields)
        {
            var text = GetHeader(orderedField);
            if (text == null)
            {
                continue;
            }

            builder.Append(text);
        }

        return builder.ToString();
    }

    internal static string ToString(string field, object? value)
    {
        var text = value == null
            ? string.Empty
            : $"{field}: {value}{WarcParser.CrLf}";
        return text;
    }

    internal virtual void SetContentBlock(byte[] contentBlock, bool isParsed = true)
    {
        /* Depending on the record's type, a content block consists of a record block and / or a
         * payload. If both exists, they are delimited by a consecutive pair of '\r\n' where the
         * first pair is found at the end of a line and the other is on its own line.
         */

        if (!isParsed)
        {
            ContentLength = contentBlock.Length;
            BlockDigest = Utils.CreateWarcDigest(DigestFactory, contentBlock);
        }
    }

    protected internal virtual void Set(string field, string value)
    {
        switch (field.ToLower())
        {
            case FieldForBlockDigest:
                BlockDigest = value;
                break;

            case FieldForContentLength:
                ContentLength = int.Parse(value);
                break;

            case FieldForDate:
                Date = DateTime.Parse(value);
                break;

            case FieldForRecordId:
                Id = Utils.RemoveBracketsFromUri(value);
                break;

            case FieldForTruncated:
                TruncatedReason = value;
                break;
        }
    }

    protected abstract string? GetHeader(string field);
}