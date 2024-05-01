﻿/*
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

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Toimik.WarcProtocol.Tests")]

namespace Toimik.WarcProtocol;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

public class MetadataRecord : Record
{
    public const string FieldForConcurrentTo = "warc-concurrent-to";

    public const string FieldForContentType = "content-type";

    public const string FieldForInfoId = "warc-warcinfo-id";

    public const string FieldForIpAddress = "warc-ip-address";

    public const string FieldForRefersTo = "warc-refers-to";

    public const string FieldForTargetUri = "warc-target-uri";

    public const string TypeName = "metadata";

    internal static readonly IEnumerable<string> DefaultOrderedFields =
    [
        FieldForType,
        FieldForRecordId,
        FieldForDate,
        FieldForContentLength,
        FieldForContentType,
        FieldForConcurrentTo,
        FieldForBlockDigest,
        FieldForIpAddress,
        FieldForRefersTo,
        FieldForTargetUri,
        FieldForTruncated,
        FieldForInfoId,
    ];

    public MetadataRecord(
        DateTime date,
        string contentBlock,
        string contentType,
        Uri infoId,
        Uri? targetUri = null,
        IPAddress? ipAddress = null,
        Uri? refersTo = null,
        ISet<Uri>? concurrentTos = null,
        string? truncatedReason = null,
        DigestFactory? digestFactory = null)
        : this(
              "1.1",
              Utils.CreateId(),
              date,
              contentBlock,
              contentType,
              infoId,
              targetUri,
              ipAddress,
              refersTo,
              concurrentTos,
              truncatedReason,
              digestFactory)
    {
    }

    public MetadataRecord(
        string version,
        Uri recordId,
        DateTime date,
        string contentBlock,
        string contentType,
        Uri infoId,
        Uri? targetUri = null,
        IPAddress? ipAddress = null,
        Uri? refersTo = null,
        ISet<Uri>? concurrentTos = null,
        string? truncatedReason = null,
        DigestFactory? digestFactory = null)
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

        InfoId = infoId;
        TargetUri = targetUri;
        IpAddress = ipAddress;
        RefersTo = refersTo;
        ConcurrentTos = concurrentTos ?? new HashSet<Uri>();
    }

    protected internal MetadataRecord(
         string version,
         Uri recordId,
         DateTime date,
         DigestFactory? digestFactory)
         : base(
               version,
               recordId,
               date,
               DefaultOrderedFields,
               digestFactory: digestFactory)
    {
        ConcurrentTos = new HashSet<Uri>();
    }

    public ISet<Uri> ConcurrentTos { get; }

    public string? ContentBlock { get; private set; }

    public string? ContentType { get; private set; }

    public Uri? InfoId { get; private set; }

    public IPAddress? IpAddress { get; private set; }

    public Uri? RefersTo { get; private set; }

    public Uri? TargetUri { get; private set; }

    public override string Type => TypeName;

    public override byte[]? GetBlockBytes() => Utils.ConvertToBytes(ContentBlock);

    internal override void SetContentBlock(byte[] contentBlock, bool isParsed = true)
    {
        base.SetContentBlock(contentBlock, isParsed);
        if (isParsed)
        {
            ContentBlock = Encoding.UTF8.GetString(contentBlock);
        }
    }

    protected internal override void Set(string field, string value)
    {
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

            case FieldForRefersTo:
                RefersTo = Utils.RemoveBracketsFromUri(value);
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

    protected override string? GetHeader(string field)
    {
        string? text = null;
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

            case FieldForInfoId:
                text = ToString("WARC-Warcinfo-ID", Utils.AddBracketsToUri(InfoId));
                break;

            case FieldForIpAddress:
                text = ToString("WARC-IP-Address", IpAddress);
                break;

            case FieldForRecordId:
                text = $"WARC-Record-ID: {Utils.AddBracketsToUri(Id)}{WarcParser.CrLf}";
                break;

            case FieldForRefersTo:
                text = ToString("WARC-Refers-To", Utils.AddBracketsToUri(RefersTo));
                break;

            case FieldForTargetUri:
                if (TargetUri != null)
                {
                    text = Utils.CreateTargetUriHeader(Version, TargetUri);
                }

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