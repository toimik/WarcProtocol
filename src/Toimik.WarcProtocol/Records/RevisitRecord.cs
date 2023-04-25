/*
 * Copyright 2021-2023 nurhafiz@hotmail.sg
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

public class RevisitRecord : Record
{
    public const string FieldForConcurrentTo = "warc-concurrent-to";

    public const string FieldForContentType = "content-type";

    public const string FieldForInfoId = "warc-warcinfo-id";

    public const string FieldForIpAddress = "warc-ip-address";

    public const string FieldForProfile = "warc-profile";

    public const string FieldForRefersTo = "warc-refers-to";

    public const string FieldForRefersToDate = "warc-refers-to-date";

    public const string FieldForRefersToTargetUri = "warc-refers-to-target-uri";

    public const string FieldForTargetUri = "warc-target-uri";

    public const string TypeName = "revisit";

    internal static readonly IEnumerable<string> DefaultOrderedFields = new List<string>
    {
        FieldForType,
        FieldForRecordId,
        FieldForDate,
        FieldForContentLength,
        FieldForContentType,
        FieldForConcurrentTo,
        FieldForBlockDigest,
        FieldForIpAddress,
        FieldForRefersTo,
        FieldForRefersToTargetUri,
        FieldForRefersToDate,
        FieldForTargetUri,
        FieldForTruncated,
        FieldForInfoId,
        FieldForProfile,
    };

    public RevisitRecord(
        DateTime date,
        string recordBlock,
        string contentType,
        Uri infoId,
        Uri targetUri,
        Uri profile,
        IPAddress? ipAddress = null,
        Uri? refersTo = null,
        DateTime? refersToDate = null,
        Uri? refersToTargetUri = null,
        ISet<Uri>? concurrentTos = null,
        string? truncatedReason = null,
        DigestFactory? digestFactory = null)
        : this(
              "1.1",
              Utils.CreateId(),
              date,
              recordBlock,
              contentType,
              infoId,
              targetUri,
              profile,
              ipAddress,
              refersTo,
              refersToDate,
              refersToTargetUri,
              concurrentTos,
              truncatedReason,
              digestFactory)
    {
    }

    public RevisitRecord(
        string version,
        Uri recordId,
        DateTime date,
        string recordBlock,
        string contentType,
        Uri infoId,
        Uri targetUri,
        Uri profile,
        IPAddress? ipAddress = null,
        Uri? refersTo = null,
        DateTime? refersToDate = null,
        Uri? refersToTargetUri = null,
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
        RecordBlock = recordBlock;
        var bytes = Encoding.UTF8.GetBytes(RecordBlock);
        var isParsed = false;
        SetContentBlock(bytes, isParsed);

        if (recordBlock.Length > 0)
        {
            ContentType = contentType;
        }

        InfoId = infoId;
        TargetUri = targetUri;
        Profile = profile;
        IpAddress = ipAddress;
        RefersTo = refersTo;
        RefersToDate = refersToDate;
        RefersToTargetUri = refersToTargetUri;
        ConcurrentTos = concurrentTos ?? new HashSet<Uri>();
    }

    internal RevisitRecord(
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

    public string? ContentType { get; private set; }

    public Uri? InfoId { get; private set; }

    public IPAddress? IpAddress { get; private set; }

    public Uri? Profile { get; private set; }

    public string? RecordBlock { get; private set; }

    public Uri? RefersTo { get; private set; }

    // Applicable to WARC 1.1
    public DateTime? RefersToDate { get; private set; }

    // Applicable to WARC 1.1
    public Uri? RefersToTargetUri { get; private set; }

    public Uri? TargetUri { get; private set; }

    public override string Type => TypeName;

    public override byte[]? GetBlockBytes() => WarcWriter.ConvertToBytes(RecordBlock);

    internal override void SetContentBlock(byte[] contentBlock, bool isParsed = true)
    {
        base.SetContentBlock(contentBlock, isParsed);
        if (isParsed)
        {
            RecordBlock = Encoding.UTF8.GetString(contentBlock);
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

            case FieldForProfile:
                Profile = Version.Equals("1.0")
                    ? Utils.RemoveBracketsFromUri(value)
                    : new(value);
                break;

            case FieldForRefersTo:
                RefersTo = Utils.RemoveBracketsFromUri(value);
                break;

            case FieldForRefersToDate:
                RefersToDate = DateTime.Parse(value);
                break;

            case FieldForRefersToTargetUri:
                RefersToTargetUri = new(value);
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

            case FieldForProfile:
                if (Profile != null)
                {
                    var profile = Version.Equals("1.0")
                        ? Utils.AddBracketsToUri(Profile)
                        : Profile.ToString();
                    text = ToString("WARC-Profile", profile);
                }

                break;

            case FieldForRecordId:
                text = $"WARC-Record-ID: {Utils.AddBracketsToUri(Id)}{WarcParser.CrLf}";
                break;

            case FieldForRefersTo:
                text = ToString("WARC-Refers-To", Utils.AddBracketsToUri(RefersTo));
                break;

            case FieldForRefersToDate:
                text = ToString("WARC-Refers-To-Date", RefersToDate);
                break;

            case FieldForRefersToTargetUri:
                text = ToString("WARC-Refers-To-Target-URI", RefersToTargetUri);
                break;

            case FieldForTargetUri:
                if (TargetUri != null)
                {
                    var targetUri = Version.Equals("1.0")
                        ? Utils.AddBracketsToUri(TargetUri)
                        : TargetUri.ToString();
                    text = ToString("WARC-Target-URI", targetUri);
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