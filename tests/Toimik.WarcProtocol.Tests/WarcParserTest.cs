namespace Toimik.WarcProtocol.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using Xunit;

public class WarcParserTest
{
    private const string FileExtensionForCompressed = ".warc.gz";

    private const string FileExtensionForUncompressed = ".warc";

    private static readonly string DirectoryForInvalidRecords = $"Data{Path.DirectorySeparatorChar}Invalid{Path.DirectorySeparatorChar}";

    private static readonly string DirectoryForValid1Point1Records = $"Data{Path.DirectorySeparatorChar}Valid{Path.DirectorySeparatorChar}1.1{Path.DirectorySeparatorChar}";

    private static readonly string DirectoryForValidRecords = $"Data{Path.DirectorySeparatorChar}Valid{Path.DirectorySeparatorChar}";

    private static readonly ISet<string> MergeFilenames = new HashSet<string>
    {
        "continuation.warc",
        "conversion.warc",
        "metadata.warc",
        "request.warc",
        "resource.warc",
        "response.warc",
        "revisit_identical.warc",
        "revisit_unmodified.warc",
        "warcinfo.warc",
    };

    [Fact]
    public async Task ExceptionDueToLongerContentLengthButIsSuppressed()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}short_content_block.warc";
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);

        Assert.Empty(records);
        var actualMessage = parseLog.Messages[0];
        Assert.Contains("Content block is", actualMessage);
    }

    [Fact]
    public async Task ExceptionDueToLongerContentLengthButIsUnsuppressed()
    {
        var parser = new WarcParser();

        var path = $"{DirectoryForInvalidRecords}short_content_block.warc";

        var ex = await Assert.ThrowsAsync<FormatException>(async () => await parser.Parse(path).ToListAsync().ConfigureAwait(false)).ConfigureAwait(false);
        Assert.Contains("Content block is", ex.Message);
    }

    [Fact]
    public async Task ExceptionDueToMalformedHeaderThatIsSuppressed()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}malformed_header.warc";
        var parseLog = new CustomParseLog();

        await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);

        var expectedMessages = new List<string>
        {
            /* NOTE: The first two are ignored because they do not start with 'warc/' */

            "Unsupported format version",
            "Invalid record declaration",
            "Invalid header field format",
            "Empty header field",
            "Missing header field for value",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
            "Duplicate header",
        };

        var messages = parseLog.Messages;
        var messageCount = messages.Count;
        Assert.Equal(expectedMessages.Count, messageCount);

        for (int i = 0; i < messageCount; i++)
        {
            var expectedMessage = expectedMessages[i];
            var actualMessage = messages[i];
            Assert.Contains(expectedMessage, actualMessage);
        }
    }

    [Fact]
    public async Task ExceptionDueToMalformedHeaderThatIsUnsuppressed()
    {
        var parser = new WarcParser();

        var path = $"{DirectoryForInvalidRecords}malformed_header.warc";

        await Assert.ThrowsAsync<FormatException>(async () => await parser.Parse(path).ToListAsync().ConfigureAwait(false)).ConfigureAwait(false);
    }

    [Fact]
    public async Task ExceptionDueToMissingMandatoryHeaderFieldButIsSuppressed()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}missing_header_field.warc";
        var parseLog = new CustomParseLog();

        await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);

        var expectedCounter = 4;
        Assert.Equal(expectedCounter, parseLog.Messages.Count);
    }

    [Fact]
    public async Task ExceptionDueToMissingMandatoryHeaderFieldButIsUnsuppressed()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}missing_header_field.warc";

        var exception = await Assert.ThrowsAsync<FormatException>(async () => await parser.Parse(path).ToListAsync().ConfigureAwait(false)).ConfigureAwait(false);
        Assert.Contains("One of the mandatory header fields is missing", exception.Message);
    }

    [Theory]
    [InlineData("premature_wo_trailing_newline.warc")]
    [InlineData("premature_w_trailing_newline.warc")]
    public async Task ExceptionDueToPrematureEndOfFileButIsSuppressed(string filename)
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}{filename}";
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);

        Assert.Empty(records);
        var actualMessage = parseLog.Messages[0];
        Assert.Contains("Premature end of file", actualMessage);
    }

    [Theory]
    [InlineData("premature_wo_trailing_newline.warc")]
    [InlineData("premature_w_trailing_newline.warc")]
    public async Task ExceptionDueToPrematureEndOfFileButIsUnsuppressed(string filename)
    {
        var parser = new WarcParser();

        var path = $"{DirectoryForInvalidRecords}{filename}";

        var ex = await Assert.ThrowsAsync<FormatException>(async () => await parser.Parse(path).ToListAsync().ConfigureAwait(false)).ConfigureAwait(false);
        Assert.Contains("Premature end of file", ex.Message);
    }

    [Fact]
    public async Task IncorrectContentLengthThatIsUncompressed()
    {
        var expectedContents = new List<string>
        {
            "Only",
            $"Here till 'WARC' of next record is read{WarcParser.CrLf}{WarcParser.CrLf}{WarcParser.CrLf}WARC",
            "foobar",
        };

        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}incorrect_content_length.warc";
        var parseLog = new CustomParseLog();
        var records = parser.Parse(path, parseLog);

        var index = 0;
        await foreach (WarcProtocol.Record record in records.ConfigureAwait(false))
        {
            var expectedContent = expectedContents[index];
            var actualContent = Encoding.UTF8.GetString(((ResourceRecord)record).RecordBlock!);
            Assert.Equal(expectedContent, actualContent);
            index++;
        }

        var chunks = parseLog.Chunks;
        var chunkCount = chunks.Count;
        Assert.Equal(2, chunkCount);

        var expectedChunks = new List<string>
        {
            $" 'Only' is read{WarcParser.CrLf}and the remaining characters (up to the start of the next record){WarcParser.CrLf}are discarded{WarcParser.CrLf}{WarcParser.CrLf}{WarcParser.CrLf}",
            @$"/1.1{WarcParser.CrLf}WARC-Date: 2000-01-01T12:34:56Z{WarcParser.CrLf}WARC-Record-ID: <urn:uuid:3a59c5c8-d806-4cdb-83aa-b21495d11063>{WarcParser.CrLf}WARC-Type: metadata{WarcParser.CrLf}Content-Length: 87{WarcParser.CrLf}{WarcParser.CrLf}This record is skipped because its header is partially consumed by the previous record.{WarcParser.CrLf}{WarcParser.CrLf}{WarcParser.CrLf}",
        };
        for (int i = 0; i < chunkCount; i++)
        {
            var expectedChunk = expectedChunks[i];
            var actualChunk = chunks[i];
            Assert.Equal(expectedChunk, actualChunk);
        }
    }

    [Theory]
    [InlineData("continuation.warc")]
    [InlineData("conversion.warc")]
    [InlineData("metadata.warc")]
    [InlineData("request.warc")]
    [InlineData("resource.warc")]
    [InlineData("response.warc")]
    [InlineData("revisit_identical.warc")]
    [InlineData("revisit_unmodified.warc")]
    [InlineData("warcinfo.warc")]
    public async Task IndividualRecordThatIsCompressed(string filename)
    {
        string? tempCompressedFile = null;
        try
        {
            var path = $"{DirectoryForValid1Point1Records}{filename}";
            tempCompressedFile = CompressFile(path);

            await TestFile(tempCompressedFile, recordCount: 1).ConfigureAwait(false);
        }
        finally
        {
            DeleteFile(tempCompressedFile);
        }
    }

    [Fact]
    public void InstantiatWithExplicitParameters()
    {
        var compressionStreamFactory = new CompressionStreamFactory();
        var recordFactory = new RecordFactory();

        var parser = new WarcParser(recordFactory, compressionStreamFactory);

        Assert.Equal(compressionStreamFactory, parser.CompressionStreamFactory);
        Assert.Equal(recordFactory, parser.RecordFactory);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MergedRecordsThatAreIndividuallyCompressedAsOneFile(bool isCustomCompressionStreamFactoryUsed)
    {
        string? path = null;
        try
        {
            path = CreateTempFile(FileExtensionForCompressed);
            MergeIndividuallyCompressedRecords(path);

            var compressionStreamFactory = isCustomCompressionStreamFactoryUsed
                ? new SharpZipCompressionStreamFactory()
                : null;
            await TestFile(
                path,
                MergeFilenames.Count,
                compressionStreamFactory).ConfigureAwait(false);
        }
        finally
        {
            DeleteFile(path);
        }
    }

    [Fact]
    public async Task MergedRecordsThatAreUncompressedAsOneFile()
    {
        string? path = null;
        try
        {
            path = CreateTempFile(FileExtensionForUncompressed);
            MergeUncompressedRecords(path);

            await TestFile(path, MergeFilenames.Count).ConfigureAwait(false);
        }
        finally
        {
            DeleteFile(path);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MergedRecordsThatAreWhollyCompressedAsOneFile(bool isCustomCompressionStreamFactoryUsed)
    {
        string? path = null;
        string? tempCompressedFile = null;
        try
        {
            path = CreateTempFile(FileExtensionForCompressed);
            MergeUncompressedRecords(path);
            tempCompressedFile = CompressFile(path);

            var compressionStreamFactory = isCustomCompressionStreamFactoryUsed
                ? new SharpZipCompressionStreamFactory()
                : null;
            await TestFile(
                tempCompressedFile,
                MergeFilenames.Count,
                compressionStreamFactory).ConfigureAwait(false);
        }
        finally
        {
            DeleteFile(tempCompressedFile);
            DeleteFile(path);
        }
    }

    [Fact]
    public async Task MultilineHeaderValues()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}multiline_header_values.warc";

        var records = await parser.Parse(path).ToListAsync().ConfigureAwait(false);
        var record = (ResourceRecord)records[0];

        Assert.Equal("1.0", record.Version);
        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
        Assert.Equal(new Uri("urn:uuid:1a59c5c8-d806-4cdb-83aa-b21495d11063"), record.Id);
        Assert.Equal(1, record.ContentLength);
        Assert.Equal("A", Encoding.UTF8.GetString(record.RecordBlock!));
    }

    [Fact]
    public async Task OffsetOverLimit()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}incorrect_content_length.warc";

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await parser.Parse(path, byteOffset: 1000).ToListAsync().ConfigureAwait(false)).ConfigureAwait(false);

        Assert.Contains("Offset exceeds file size", exception.Message);
    }

    [Fact]
    public async Task OffsetUnderLimit()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}incorrect_content_length.warc";

        var records = await parser.Parse(path, byteOffset: 697).ToListAsync().ConfigureAwait(false);
        var record = (ResourceRecord)records[0];

        Assert.Equal("1.1", record.Version);
        Assert.Equal(DateTime.Parse("2001-01-01T12:34:56Z"), record.Date);
        Assert.Equal(new Uri("urn:uuid:1a59c5c8-d806-4cdb-83aa-b21495d11063"), record.Id);
        Assert.Equal(ResourceRecord.TypeName, record.Type);
        Assert.Equal(6, record.ContentLength);
        Assert.Equal("foobar", Encoding.UTF8.GetString(record.RecordBlock!));
    }

    [Theory]
    [InlineData("1.0", false)]
    [InlineData("1.0", true)]
    [InlineData("1.1", false)]
    [InlineData("1.1", true)]
    public async Task RecordForContinuationThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "continuation.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);
        var actualRecord = (ContinuationRecord)records[0];

        var digestFactory = CreateDigestFactory(actualRecord.BlockDigest);
        var expectedRecord = new ContinuationRecord(
            actualRecord.Version,
            actualRecord.Id,
            actualRecord.Date,
            actualRecord.RecordBlock!,
            actualRecord.PayloadDigest!,
            actualRecord.InfoId!,
            actualRecord.TargetUri!,
            actualRecord.SegmentOriginId!,
            actualRecord.SegmentNumber,
            actualRecord.SegmentTotalLength,
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.RecordBlock!.Length, actualRecord.ContentLength);
        Assert.Equal(expectedRecord.RecordBlock, actualRecord.RecordBlock);

        AssertContinuationRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = ContinuationRecord.DefaultOrderedFields;
        if (isWithoutBlockDigest)
        {
            orderedFields = new List<string>(orderedFields);
            ((List<string>)orderedFields).Remove(WarcProtocol.Record.FieldForBlockDigest);
        }

        var fields = AssertHeaderAndToString(actualRecord, orderedFields);

        fields.Sort();
        var expectedHeader = expectedRecord.GetHeader(fields);
        var actualHeader = actualRecord.GetHeader(fields);
        Assert.Equal(expectedHeader, actualHeader);
    }

    [Theory]
    [InlineData("1.0", false)]
    [InlineData("1.0", true)]
    [InlineData("1.1", false)]
    [InlineData("1.1", true)]
    public async Task RecordForConversionThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "conversion.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);
        var actualRecord = (ConversionRecord)records[0];

        var digestFactory = CreateDigestFactory(actualRecord.BlockDigest);
        var expectedRecord = new ConversionRecord(
            actualRecord.Version,
            actualRecord.Id,
            actualRecord.Date,
            actualRecord.PayloadTypeIdentifier,
            actualRecord.RecordBlock!,
            actualRecord.ContentType!,
            actualRecord.InfoId!,
            actualRecord.TargetUri!,
            actualRecord.PayloadDigest,
            actualRecord.RefersTo,
            actualRecord.IsSegmented(),
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.RecordBlock!.Length, actualRecord.ContentLength);
        Assert.Null(expectedRecord.IdentifiedPayloadType);

        // NOTE: See remarks #1
        Assert.Null(actualRecord.IdentifiedPayloadType);

        AssertConversionRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = ConversionRecord.DefaultOrderedFields;
        if (isWithoutBlockDigest)
        {
            orderedFields = new List<string>(orderedFields);
            ((List<string>)orderedFields).Remove(WarcProtocol.Record.FieldForBlockDigest);
        }

        var fields = AssertHeaderAndToString(
            actualRecord,
            orderedFields,
            hasIgnoredIdentifiedPayloadType: true);

        fields.Sort();
        var expectedHeader = expectedRecord.GetHeader(fields);
        var actualHeader = actualRecord.GetHeader(fields);
        Assert.Equal(expectedHeader, actualHeader);
    }

    [Theory]
    [InlineData("1.0", false)]
    [InlineData("1.0", true)]
    [InlineData("1.1", false)]
    [InlineData("1.1", true)]
    public async Task RecordForMetadataThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "metadata.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);
        var actualRecord = (MetadataRecord)records[0];

        var digestFactory = CreateDigestFactory(actualRecord.BlockDigest);
        var expectedRecord = new MetadataRecord(
            actualRecord.Version,
            actualRecord.Id,
            actualRecord.Date,
            actualRecord.ContentBlock!,
            actualRecord.ContentType!,
            actualRecord.InfoId!,
            actualRecord.TargetUri,
            actualRecord.IpAddress,
            actualRecord.RefersTo,
            actualRecord.ConcurrentTos,
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.ContentBlock!.Length, actualRecord.ContentLength);
        Assert.Equal(expectedRecord.ContentBlock, actualRecord.ContentBlock);

        AssertMetadataRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = MetadataRecord.DefaultOrderedFields;
        if (isWithoutBlockDigest)
        {
            orderedFields = new List<string>(orderedFields);
            ((List<string>)orderedFields).Remove(WarcProtocol.Record.FieldForBlockDigest);
        }

        var fields = AssertHeaderAndToString(actualRecord, orderedFields);

        fields.Sort();
        var expectedHeader = expectedRecord.GetHeader(fields);
        var actualHeader = actualRecord.GetHeader(fields);
        Assert.Equal(expectedHeader, actualHeader);
    }

    [Theory]
    [InlineData("1.0", false)]
    [InlineData("1.0", true)]
    [InlineData("1.1", false)]
    [InlineData("1.1", true)]
    public async Task RecordForRequestThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "request.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);
        var actualRecord = (RequestRecord)records[0];

        var digestFactory = CreateDigestFactory(actualRecord.BlockDigest);
        var expectedRecord = new RequestRecord(
            actualRecord.Version,
            actualRecord.Id,
            actualRecord.Date,
            actualRecord.PayloadTypeIdentifier,
            actualRecord.ContentBlock!,
            actualRecord.ContentType!,
            actualRecord.InfoId!,
            actualRecord.TargetUri!,
            actualRecord.PayloadDigest,
            actualRecord.IpAddress,
            actualRecord.ConcurrentTos,
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.ContentBlock!.Length, actualRecord.ContentLength);
        Assert.Null(expectedRecord.IdentifiedPayloadType);
        Assert.Null(actualRecord.IdentifiedPayloadType);

        AssertRequestRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = RequestRecord.DefaultOrderedFields;
        if (isWithoutBlockDigest)
        {
            orderedFields = new List<string>(orderedFields);
            ((List<string>)orderedFields).Remove(WarcProtocol.Record.FieldForBlockDigest);
        }

        var fields = AssertHeaderAndToString(
            actualRecord,
            orderedFields,
            hasIgnoredIdentifiedPayloadType: true);

        fields.Sort();
        var expectedHeader = expectedRecord.GetHeader(fields);
        var actualHeader = actualRecord.GetHeader(fields);
        Assert.Equal(expectedHeader, actualHeader);
    }

    [Theory]
    [InlineData("1.0", false)]
    [InlineData("1.0", true)]
    [InlineData("1.1", false)]
    [InlineData("1.1", true)]
    public async Task RecordForResourceThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "resource.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);
        var actualRecord = (ResourceRecord)records[0];

        var digestFactory = CreateDigestFactory(actualRecord.BlockDigest);
        var expectedRecord = new ResourceRecord(
            actualRecord.Version,
            actualRecord.Id,
            actualRecord.Date,
            actualRecord.PayloadTypeIdentifier,
            actualRecord.RecordBlock!,
            actualRecord.ContentType!,
            actualRecord.InfoId!,
            actualRecord.TargetUri!,
            actualRecord.PayloadDigest,
            actualRecord.IpAddress,
            actualRecord.ConcurrentTos,
            actualRecord.IsSegmented(),
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.RecordBlock!.Length, actualRecord.ContentLength);
        Assert.Null(actualRecord.IdentifiedPayloadType);

        // NOTE: See remarks #1
        Assert.Null(actualRecord.IdentifiedPayloadType);

        AssertResourceRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = ResourceRecord.DefaultOrderedFields;
        if (isWithoutBlockDigest)
        {
            orderedFields = new List<string>(orderedFields);
            ((List<string>)orderedFields).Remove(WarcProtocol.Record.FieldForBlockDigest);
        }

        var fields = AssertHeaderAndToString(
            actualRecord,
            orderedFields,
            hasIgnoredIdentifiedPayloadType: true);

        fields.Sort();
        var expectedHeader = expectedRecord.GetHeader(fields);
        var actualHeader = actualRecord.GetHeader(fields);
        Assert.Equal(expectedHeader, actualHeader);
    }

    [Theory]
    [InlineData("1.0", false)]
    [InlineData("1.0", true)]
    [InlineData("1.1", false)]
    [InlineData("1.1", true)]
    public async Task RecordForResponseThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "response.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);
        var actualRecord = (ResponseRecord)records[0];

        var digestFactory = CreateDigestFactory(actualRecord.BlockDigest);
        var expectedRecord = new ResponseRecord(
            actualRecord.Version,
            actualRecord.Id,
            actualRecord.Date,
            actualRecord.PayloadTypeIdentifier,
            actualRecord.ContentBlock!,
            actualRecord.ContentType!,
            actualRecord.InfoId!,
            actualRecord.TargetUri!,
            actualRecord.PayloadDigest,
            actualRecord.IpAddress,
            actualRecord.ConcurrentTos,
            actualRecord.IsSegmented(),
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.ContentBlock!.Length, actualRecord.ContentLength);
        Assert.Null(expectedRecord.IdentifiedPayloadType);

        // NOTE: See remarks #1
        Assert.Null(actualRecord.IdentifiedPayloadType);

        AssertResponseRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = ResponseRecord.DefaultOrderedFields;
        if (isWithoutBlockDigest)
        {
            orderedFields = new List<string>(orderedFields);
            ((List<string>)orderedFields).Remove(WarcProtocol.Record.FieldForBlockDigest);
        }

        var fields = AssertHeaderAndToString(
            actualRecord,
            orderedFields,
            hasIgnoredIdentifiedPayloadType: true);

        fields.Sort();
        var expectedHeader = expectedRecord.GetHeader(fields);
        var actualHeader = actualRecord.GetHeader(fields);
        Assert.Equal(expectedHeader, actualHeader);
    }

    [Theory]
    [InlineData("1.0", false)]
    [InlineData("1.0", true)]
    [InlineData("1.1", false)]
    [InlineData("1.1", true)]
    public async Task RecordForRevisitOfIdenticalThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "revisit_identical.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);
        var actualRecord = (RevisitRecord)records[0];

        var digestFactory = CreateDigestFactory(actualRecord.BlockDigest);
        var expectedRecord = new RevisitRecord(
            actualRecord.Version,
            actualRecord.Id,
            actualRecord.Date,
            actualRecord.RecordBlock!,
            actualRecord.ContentType!,
            actualRecord.InfoId!,
            actualRecord.TargetUri!,
            actualRecord.Profile!,
            actualRecord.IpAddress,
            actualRecord.RefersTo,
            actualRecord.RefersToDate,
            actualRecord.RefersToTargetUri,
            actualRecord.ConcurrentTos,
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.RecordBlock!.Length, actualRecord.ContentLength);
        Assert.Equal(expectedRecord.RecordBlock, actualRecord.RecordBlock);

        AssertRevisitRecordForIdentical(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = RevisitRecord.DefaultOrderedFields;
        if (isWithoutBlockDigest)
        {
            orderedFields = new List<string>(orderedFields);
            ((List<string>)orderedFields).Remove(WarcProtocol.Record.FieldForBlockDigest);
        }

        var fields = AssertHeaderAndToString(actualRecord, orderedFields);

        fields.Sort();
        var expectedHeader = expectedRecord.GetHeader(fields);

        // NOTE: Parsing warc files will preserve the value of Content-Type, if any. However,
        // creating a record with a Content-Length of zero will cause a non-null Content-Type to
        // be null. Thus, the Content-Type for this record is removed.
        fields.Remove(RevisitRecord.FieldForContentType);

        var actualHeader = actualRecord.GetHeader(fields);
        Assert.Equal(expectedHeader, actualHeader);
    }

    [Theory]
    [InlineData("1.0", false)]
    [InlineData("1.0", true)]
    [InlineData("1.1", false)]
    [InlineData("1.1", true)]
    public async Task RecordForRevisitOfUnmodifiedThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "revisit_unmodified.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);
        var actualRecord = (RevisitRecord)records[0];

        var digestFactory = CreateDigestFactory(actualRecord.BlockDigest);
        var expectedRecord = new RevisitRecord(
            actualRecord.Version,
            actualRecord.Id,
            actualRecord.Date,
            actualRecord.RecordBlock!,
            actualRecord.ContentType!,
            actualRecord.InfoId!,
            actualRecord.TargetUri!,
            actualRecord.Profile!,
            actualRecord.IpAddress,
            actualRecord.RefersTo,
            actualRecord.RefersToDate,
            actualRecord.RefersToTargetUri,
            actualRecord.ConcurrentTos,
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.RecordBlock!.Length, actualRecord.ContentLength);
        Assert.Equal(expectedRecord.RecordBlock, actualRecord.RecordBlock);

        AssertRevisitRecordForUnmodified(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = RevisitRecord.DefaultOrderedFields;
        if (isWithoutBlockDigest)
        {
            orderedFields = new List<string>(orderedFields);
            ((List<string>)orderedFields).Remove(WarcProtocol.Record.FieldForBlockDigest);
        }

        var fields = AssertHeaderAndToString(actualRecord, orderedFields);

        fields.Sort();
        var expectedHeader = expectedRecord.GetHeader(fields);
        var actualHeader = actualRecord.GetHeader(fields);
        Assert.Equal(expectedHeader, actualHeader);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecordForWarcinfoThatIsUncompressed(bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "warcinfo.warc",
            version: "1.1",
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync().ConfigureAwait(false);
        var actualRecord = (WarcinfoRecord)records[0];

        var digestFactory = CreateDigestFactory(actualRecord.BlockDigest);
        var expectedRecord = new WarcinfoRecord(
            actualRecord.Version,
            actualRecord.Id,
            actualRecord.Date,
            actualRecord.ContentBlock!,
            actualRecord.ContentType!,
            actualRecord.Filename,
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.ContentBlock!.Length, actualRecord.ContentLength);
        Assert.Equal(expectedRecord.ContentBlock, actualRecord.ContentBlock);

        AssertWarcinfoRecord(actualRecord, isWithoutBlockDigest);

        var orderedFields = WarcinfoRecord.DefaultOrderedFields;
        if (isWithoutBlockDigest)
        {
            orderedFields = new List<string>(orderedFields);
            ((List<string>)orderedFields).Remove(WarcProtocol.Record.FieldForBlockDigest);
        }

        var fields = AssertHeaderAndToString(actualRecord, orderedFields);

        fields.Sort();
        var expectedHeader = expectedRecord.GetHeader(fields);
        var actualHeader = actualRecord.GetHeader(fields);
        Assert.Equal(expectedHeader, actualHeader);
    }

    private static void AssertContinuationRecord(
        ContinuationRecord record,
        string version,
        bool isWithoutBlockDigest = false)
    {
        Assert.Equal(version, record.Version);
        if (!isWithoutBlockDigest)
        {
            Assert.Equal("sha1:C767A59C4DBC7430855F7BF1468D495376E92C3D", record.BlockDigest);
        }

        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
        Assert.Equal("sha1:CB927B7C7DE7DA663A68973E0C034FFBE6A98BF4", record.PayloadDigest);
        Assert.Equal(new Uri("urn:uuid:6a59c5c8-d806-4cdb-83aa-b21495d11063"), record.Id);
        Assert.Equal(2, record.SegmentNumber);
        Assert.Equal(new Uri("urn:uuid:a6ddea17-518c-44d7-8d34-77f8ef0d0890"), record.SegmentOriginId);
        Assert.Equal(217, record.SegmentTotalLength);
        Assert.Equal(new Uri("dns://example.com"), record.TargetUri);
        Assert.Equal(ContinuationRecord.TypeName, record.Type);
        Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
        Assert.Equal(83, record.ContentLength);
    }

    private static void AssertConversionRecord(
        ConversionRecord record,
        string version,
        bool isWithoutBlockDigest = false)
    {
        Assert.Equal(version, record.Version);
        if (!isWithoutBlockDigest)
        {
            Assert.Equal("sha1:14688051DB31569CB48D9C36C818B593A5228916", record.BlockDigest);
        }

        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
        Assert.Equal(new Uri("urn:uuid:07d18ee8-5a5e-43e1-9ff9-320a994141fd"), record.Id);
        Assert.Equal(new Uri("urn:uuid:a6ddea17-518c-44d7-8d34-77f8ef0d0890"), record.RefersTo);
        Assert.Equal(1, record.SegmentNumber);
        Assert.Equal(new Uri("file://var/www/htdoc/robots.txt"), record.TargetUri);
        Assert.Equal(ConversionRecord.TypeName, record.Type);
        Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
        Assert.Equal(41, record.ContentLength);
        Assert.Equal("text/plain", record.ContentType);
        var actualContentType = new ContentTypeIdentifier().Identify(record);
        Assert.Equal("application/octet-stream", actualContentType);
    }

    private static List<string> AssertHeaderAndToString(
        WarcProtocol.Record record,
        IEnumerable<string> defaultOrderedFields,
        bool hasIgnoredIdentifiedPayloadType = false)
    {
        var expectedHeader = record.GetHeader();
        var expectedHeaderTokens = expectedHeader.Split(WarcParser.CrLf, StringSplitOptions.RemoveEmptyEntries);
        var actualFields = new List<string>(defaultOrderedFields);

        // NOTE: WARC-Identified-Payload-Type, if any, is intentionally ignored by the parser
        // because that value must be independently generated. As the feature is not
        // implemented, the header will not include the field-value pair.
        var headerDeclarationAndFieldCount = hasIgnoredIdentifiedPayloadType
            ? actualFields.Count
            : actualFields.Count + 1;
        Assert.Equal(expectedHeaderTokens.Length, headerDeclarationAndFieldCount);

        var actualHeader = record.GetHeader(actualFields);
        var actualHeaderTokens = actualHeader.Split(WarcParser.CrLf, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(headerDeclarationAndFieldCount, actualHeaderTokens.Length);

        return actualFields;
    }

    private static void AssertMetadataRecord(
        MetadataRecord record,
        string version,
        bool isWithoutBlockDigest = false)
    {
        Assert.Equal(version, record.Version);
        if (!isWithoutBlockDigest)
        {
            Assert.Equal("sha1:25EEDCA9AC3593DBDB2E3B5D69219231A8824257", record.BlockDigest);
        }

        Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:25809bd7-a7e7-4a97-81b5-d7d9747f3eaf")));
        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
        Assert.Equal(new Uri("urn:uuid:90c6fd91-f744-4818-95d1-693f933d2214"), record.Id);
        Assert.Equal(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221"), record.RefersTo);
        Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
        Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
        Assert.Equal(MetadataRecord.TypeName, record.Type);
        Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
        Assert.Equal(62, record.ContentLength);
        var actualContentType = new ContentTypeIdentifier().Identify(record);
        Assert.Equal(record.ContentType, actualContentType);
    }

    private static void AssertRequestRecord(
        RequestRecord record,
        string version,
        bool isWithoutBlockDigest = false)
    {
        Assert.Equal(version, record.Version);
        if (!isWithoutBlockDigest)
        {
            Assert.Equal("sha1:85FFD42680A33B720124A4D29E6CB4F8C2798C92", record.BlockDigest);
        }

        Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221")));
        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
        Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
        Assert.Equal("sha1:DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", record.PayloadDigest);
        Assert.Equal(new Uri("urn:uuid:672baea5-b4cd-49e3-9ce7-6cdf93513d54"), record.Id);
        Assert.Equal(RequestRecord.TypeName, record.Type);
        Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
        Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
        Assert.Equal(190, record.ContentLength);
        var actualContentType = new ContentTypeIdentifier().Identify(record);
        Assert.Equal(record.ContentType, actualContentType);
    }

    private static void AssertResourceRecord(
        ResourceRecord record,
        string version,
        bool isWithoutBlockDigest = false)
    {
        Assert.Equal(version, record.Version);
        if (!isWithoutBlockDigest)
        {
            Assert.Equal("sha1:231AD621E019B0108B5886A3ABFDDB6D8F9477D2", record.BlockDigest);
        }

        Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:25809bd7-a7e7-4a97-81b5-d7d9747f3eaf")));
        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
        Assert.Equal(IPAddress.Parse("127.0.0.1"), record.IpAddress);
        Assert.Equal("sha1:CB927B7C7DE7DA663A68973E0C034FFBE6A98BF4", record.PayloadDigest);
        Assert.Equal(new Uri("urn:uuid:a6ddea17-518c-44d7-8d34-77f8ef0d0890"), record.Id);
        Assert.Equal(new Uri("dns://example.com"), record.TargetUri);
        Assert.Equal(ResourceRecord.TypeName, record.Type);
        Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
        Assert.Equal(174, record.ContentLength);
        Assert.Equal(1, record.SegmentNumber);
        Assert.Equal("text/dns", record.ContentType);
        var actualContentType = new ContentTypeIdentifier().Identify(record);
        Assert.Equal("text/dns", actualContentType);
    }

    private static void AssertResponseRecord(
        ResponseRecord record,
        string version,
        bool isWithoutBlockDigest = false)
    {
        Assert.Equal(version, record.Version);
        if (!isWithoutBlockDigest)
        {
            Assert.Equal("sha1:335680BF19511379DB8BAB0C3F3D92318751C0C4", record.BlockDigest);
        }

        Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:672baea5-b4cd-49e3-9ce7-6cdf93513d54")));
        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
        Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
        Assert.Equal("sha1:D25B43EB7F3483D5C2D8891C195A7F957EF9159C", record.PayloadDigest);
        Assert.Equal(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221"), record.Id);
        Assert.Equal(1, record.SegmentNumber);
        Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
        Assert.Equal(ResponseRecord.TypeName, record.Type);
        Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
        Assert.Equal(1679, record.ContentLength);
        var actualContentType = new ContentTypeIdentifier().Identify(record);
        Assert.Equal(record.ContentType, actualContentType);
    }

    private static void AssertRevisitRecordForIdentical(
        RevisitRecord record,
        string version,
        bool isWithoutBlockDigest = false)
    {
        Assert.Equal(version, record.Version);
        if (!isWithoutBlockDigest)
        {
            Assert.Equal("sha1:DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", record.BlockDigest);
        }

        Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221")));
        Assert.Equal(DateTime.Parse("2001-02-01T01:23:45Z"), record.Date);
        Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
        Assert.Equal(new Uri("http://netpreserve.org/warc/1.1/revisit/identical-payload-digest"), record.Profile);
        Assert.Equal(new Uri("urn:uuid:c48e2242-219c-45b1-b0c3-ecc6b784e4ed"), record.Id);
        Assert.Equal(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221"), record.RefersTo);
        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.RefersToDate);
        Assert.Equal(new Uri("http://www.example.com"), record.RefersToTargetUri);
        Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
        Assert.Equal(RevisitRecord.TypeName, record.Type);
        Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
        Assert.Equal(0, record.ContentLength);
        Assert.Equal("irrelevant/but-still-parsed-for-preservation", record.ContentType);
        var actualContentType = new ContentTypeIdentifier().Identify(record);
        Assert.Equal("application/octet-stream", actualContentType);
    }

    private static void AssertRevisitRecordForUnmodified(
        RevisitRecord record,
        string version,
        bool isWithoutBlockDigest = false)
    {
        Assert.Equal(version, record.Version);
        if (!isWithoutBlockDigest)
        {
            Assert.Equal("sha1:241E0040D1186D236DFC2DDCC80BD712DE038268", record.BlockDigest);
        }

        Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221")));
        Assert.Equal(DateTime.Parse("2002-03-01T03:45:06Z"), record.Date);
        Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
        Assert.Equal(new Uri("http://netpreserve.org/warc/1.1/revisit/server-not-modified"), record.Profile);
        Assert.Equal(new Uri("urn:uuid:e8381eac-7fdf-4b53-ad69-6943472920f9"), record.Id);
        Assert.Equal(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221"), record.RefersTo);
        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.RefersToDate);
        Assert.Equal(new Uri("http://www.example.com"), record.RefersToTargetUri);
        Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
        Assert.Equal(RevisitRecord.TypeName, record.Type);
        Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
        Assert.Equal(198, record.ContentLength);
        Assert.Equal("message/http", record.ContentType);
        var actualContentType = new ContentTypeIdentifier().Identify(record);
        Assert.Equal("application/octet-stream", actualContentType);
    }

    private static void AssertWarcinfoRecord(WarcinfoRecord record, bool isWithoutBlockDigest = false)
    {
        Assert.Equal("1.1", record.Version);
        if (!isWithoutBlockDigest)
        {
            Assert.Equal("sha1:11C37A02E327EDE38A916B7153DE2CC6DC7876EB", record.BlockDigest);
        }

        Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
        Assert.Equal("warcinfo.warc", record.Filename);
        Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.Id);
        Assert.Equal(WarcinfoRecord.TypeName, record.Type);
        Assert.Equal(241, record.ContentLength);
        var actualContentType = new ContentTypeIdentifier().Identify(record);
        Assert.Equal(record.ContentType, actualContentType);
    }

    private static string CompressFile(string path)
    {
        using var inputStream = File.OpenRead(path);
        var tempPath = CreateTempFile(FileExtensionForCompressed);
        using var outputStream = File.OpenWrite(tempPath);
        using (GZipStream compressedStream = new(outputStream, CompressionMode.Compress))
        {
            inputStream.CopyTo(compressedStream);
        }

        return tempPath;
    }

    private static DigestFactory? CreateDigestFactory(string? blockDigest)
    {
        DigestFactory? digestFactory;
        if (blockDigest == null)
        {
            digestFactory = null;
        }
        else
        {
            var index = blockDigest.IndexOf(':');
            var hashName = blockDigest[..index].TrimStart();
            digestFactory = new DigestFactory(hashName);
        }

        return digestFactory;
    }

    private static string CreatePath(
        string filename,
        string version,
        bool isWithoutBlockDigest)
    {
        var path = $"{DirectoryForValidRecords}{version}{Path.DirectorySeparatorChar}";
        if (isWithoutBlockDigest)
        {
            path = $"{path}without-block-digest{Path.DirectorySeparatorChar}";
        }

        path = $"{path}{filename}";
        return path;
    }

    [ExcludeFromCodeCoverage]
    private static string CreateTempFile(string fileExtension)
    {
        string path;
        string? tempFilename = null;
        try
        {
            do
            {
                tempFilename = Path.GetTempFileName();
                path = RenameFileExtension(tempFilename, fileExtension);
                try
                {
                    File.Move(tempFilename, path);
                    break;
                }
                catch (IOException)
                {
                    // Do nothing
                }
            }
            while (true);
        }
        finally
        {
            DeleteFile(tempFilename);
        }

        return path;
    }

    [ExcludeFromCodeCoverage]
    private static void DeleteFile(string? path)
    {
        if (path != null)
        {
            // Repeatedly try to delete the file until successful
            do
            {
                try
                {
                    File.Delete(path);
                    break;
                }
                catch (IOException ex)
                {
                    if (!ex.Message.StartsWith("The process cannot access the file"))
                    {
                        throw;
                    }
                }
            }
            while (true);
        }
    }

    private static void MergeIndividuallyCompressedRecords(string path)
    {
        using var outputStream = File.OpenWrite(path);
        using var compressedStream = new GZipStream(outputStream, CompressionMode.Compress);
        foreach (string filename in MergeFilenames)
        {
            var tempPath = $"{DirectoryForValid1Point1Records}{filename}";
            using var inputStream = File.OpenRead(tempPath);
            inputStream.CopyTo(compressedStream);
        }
    }

    private static void MergeUncompressedRecords(string path)
    {
        using var outputStream = File.OpenWrite(path);
        foreach (string filename in MergeFilenames)
        {
            var tempPath = $"{DirectoryForValid1Point1Records}{filename}";
            using var inputStream = File.OpenRead(tempPath);
            inputStream.CopyTo(outputStream);
        }
    }

    private static string RenameFileExtension(string path, string extension)
    {
        var slashIndex = path.LastIndexOf(Path.DirectorySeparatorChar);
        var periodIndex = path.LastIndexOf(".");
        var length = periodIndex - slashIndex;
        var filename = path.Substring(slashIndex, length);
        var directory = path[..slashIndex];
        var newPath = $"{directory}{filename}{extension}";
        return newPath;
    }

    private static async Task TestFile(
        string path,
        int recordCount,
        CompressionStreamFactory? compressionStreamFactory = null)
    {
        var recordCounter = 0;
        var parser = new WarcParser(compressionStreamFactory: compressionStreamFactory);
        await foreach (WarcProtocol.Record record in parser.Parse(path).ConfigureAwait(false))
        {
            switch (record.Type)
            {
                case ContinuationRecord.TypeName:
                    AssertContinuationRecord((ContinuationRecord)record, version: "1.1");
                    recordCounter++;
                    break;

                case ConversionRecord.TypeName:
                    var conversionRecord = (ConversionRecord)record;

                    // NOTE: See remarks #1
                    Assert.Null(conversionRecord.IdentifiedPayloadType);

                    AssertConversionRecord(conversionRecord, version: "1.1");
                    recordCounter++;
                    break;

                case MetadataRecord.TypeName:
                    AssertMetadataRecord((MetadataRecord)record, version: "1.1");
                    recordCounter++;
                    break;

                case RequestRecord.TypeName:
                    var requestRecord = (RequestRecord)record;
                    Assert.Null(requestRecord.IdentifiedPayloadType);
                    AssertRequestRecord(requestRecord, version: "1.1");
                    recordCounter++;
                    break;

                case ResourceRecord.TypeName:
                    AssertResourceRecord((ResourceRecord)record, version: "1.1");
                    recordCounter++;
                    break;

                case ResponseRecord.TypeName:
                    var responseRecord = (ResponseRecord)record;

                    // NOTE: See remarks #1
                    Assert.Null(responseRecord.IdentifiedPayloadType);

                    AssertResponseRecord(responseRecord, version: "1.1");
                    recordCounter++;
                    break;

                case RevisitRecord.TypeName:
                    var revisitRecord = (RevisitRecord)record;
                    if (revisitRecord.ContentLength == 0)
                    {
                        AssertRevisitRecordForIdentical(revisitRecord, version: "1.1");
                    }
                    else
                    {
                        AssertRevisitRecordForUnmodified(revisitRecord, version: "1.1");
                    }

                    recordCounter++;
                    break;

                case WarcinfoRecord.TypeName:
                    AssertWarcinfoRecord((WarcinfoRecord)record);
                    recordCounter++;
                    break;
            }
        }

        Assert.Equal(recordCount, recordCounter);
    }

    private class CustomParseLog : IParseLog
    {
        public IList<string> Chunks { get; private set; } = new List<string>();

        public IList<string> Messages { get; private set; } = new List<string>();

        public void ChunkSkipped(string chunk)
        {
            Chunks.Add(chunk);
        }

        public void ErrorEncountered(string error)
        {
            Messages.Add(error);
        }
    }

    private class SharpZipCompressionStreamFactory : CompressionStreamFactory
    {
        public override Stream CreateDecompressStream(Stream stream)
        {
            return new GZipInputStream(stream);
        }
    }
}

// NOTE: (Remarks #1) Although the value is specified, the parser ignores it because the value must
// be independently identified using PayloadTypeIdentifier.Identify(...), which is not yet
// implemented