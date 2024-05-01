namespace Toimik.WarcProtocol.Tests;

using ICSharpCode.SharpZipLib.GZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class WarcParserTest
{
    public static readonly string DirectoryForValidRecords = $"Data{Path.DirectorySeparatorChar}Valid{Path.DirectorySeparatorChar}";

    private const string FileExtensionForCompressed = ".warc.gz";

    private const string FileExtensionForUncompressed = ".warc";

    private static readonly string DirectoryForInvalidRecords = $"Data{Path.DirectorySeparatorChar}Invalid{Path.DirectorySeparatorChar}";

    private static readonly string DirectoryForValid1Point1Records = $"Data{Path.DirectorySeparatorChar}Valid{Path.DirectorySeparatorChar}1.1{Path.DirectorySeparatorChar}";

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

        var records = await parser.Parse(path, parseLog).ToListAsync();

        Assert.Empty(records);
        var actualMessage = parseLog.Messages[0];
        Assert.Contains("Content block is", actualMessage);
    }

    [Fact]
    public async Task ExceptionDueToLongerContentLengthButIsUnsuppressed()
    {
        var parser = new WarcParser();

        var path = $"{DirectoryForInvalidRecords}short_content_block.warc";

        var ex = await Assert.ThrowsAsync<FormatException>(async () => await parser.Parse(path).ToListAsync());
        Assert.Contains("Content block is", ex.Message);
    }

    [Fact]
    public async Task ExceptionDueToMalformedHeaderThatIsSuppressed()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}malformed_header.warc";
        var parseLog = new CustomParseLog();

        await parser.Parse(path, parseLog).ToListAsync();

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

        await Assert.ThrowsAsync<FormatException>(async () => await parser.Parse(path).ToListAsync());
    }

    [Fact]
    public async Task ExceptionDueToMissingMandatoryHeaderFieldButIsSuppressed()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}missing_header_field.warc";
        var parseLog = new CustomParseLog();

        await parser.Parse(path, parseLog).ToListAsync();

        var expectedCounter = 4;
        Assert.Equal(expectedCounter, parseLog.Messages.Count);
    }

    [Fact]
    public async Task ExceptionDueToMissingMandatoryHeaderFieldButIsUnsuppressed()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}missing_header_field.warc";

        var exception = await Assert.ThrowsAsync<FormatException>(async () => await parser.Parse(path).ToListAsync());
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

        var records = await parser.Parse(path, parseLog).ToListAsync();

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

        var ex = await Assert.ThrowsAsync<FormatException>(async () => await parser.Parse(path).ToListAsync());
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
        await foreach (WarcProtocol.Record record in records)
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

            await TestUtils.TestFile(tempCompressedFile, recordCount: 1);
        }
        finally
        {
            TestUtils.DeleteFile(tempCompressedFile);
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
            path = TestUtils.CreateTempFile(FileExtensionForCompressed);
            MergeIndividuallyCompressedRecords(path);

            var compressionStreamFactory = isCustomCompressionStreamFactoryUsed
                ? new SharpZipCompressionStreamFactory()
                : null;
            await TestUtils.TestFile(
                path,
                MergeFilenames.Count,
                compressionStreamFactory);
        }
        finally
        {
            TestUtils.DeleteFile(path);
        }
    }

    [Fact]
    public async Task MergedRecordsThatAreUncompressedAsOneFile()
    {
        string? path = null;
        try
        {
            path = TestUtils.CreateTempFile(FileExtensionForUncompressed);
            MergeUncompressedRecords(path);

            await TestUtils.TestFile(path, MergeFilenames.Count);
        }
        finally
        {
            TestUtils.DeleteFile(path);
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
            path = TestUtils.CreateTempFile(FileExtensionForCompressed);
            MergeUncompressedRecords(path);
            tempCompressedFile = CompressFile(path);

            var compressionStreamFactory = isCustomCompressionStreamFactoryUsed
                ? new SharpZipCompressionStreamFactory()
                : null;
            await TestUtils.TestFile(
                tempCompressedFile,
                MergeFilenames.Count,
                compressionStreamFactory);
        }
        finally
        {
            TestUtils.DeleteFile(tempCompressedFile);
            TestUtils.DeleteFile(path);
        }
    }

    [Fact]
    public async Task MultilineHeaderValues()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}multiline_header_values.warc";

        var records = await parser.Parse(path).ToListAsync();
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

        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await parser.Parse(path, byteOffset: 1000).ToListAsync());

        Assert.Contains("Offset exceeds file size", exception.Message);
    }

    [Fact]
    public async Task OffsetUnderLimit()
    {
        var parser = new WarcParser();
        var path = $"{DirectoryForInvalidRecords}incorrect_content_length.warc";

        var records = await parser.Parse(path, byteOffset: 697).ToListAsync();
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

        var records = await parser.Parse(path, parseLog).ToListAsync();
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
            actualRecord.IdentifiedPayloadType,
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.RecordBlock!.Length, actualRecord.ContentLength);
        Assert.Equal(expectedRecord.RecordBlock, actualRecord.RecordBlock);

        TestUtils.AssertContinuationRecord(
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

        var records = await parser.Parse(path, parseLog).ToListAsync();
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
            actualRecord.IdentifiedPayloadType,
            actualRecord.RefersTo,
            actualRecord.IsSegmented(),
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.RecordBlock!.Length, actualRecord.ContentLength);

        TestUtils.AssertConversionRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = ConversionRecord.DefaultOrderedFields;
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
    public async Task RecordForMetadataThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "metadata.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync();
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

        TestUtils.AssertMetadataRecord(
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

        var records = await parser.Parse(path, parseLog).ToListAsync();
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
            actualRecord.IdentifiedPayloadType,
            actualRecord.IpAddress,
            actualRecord.ConcurrentTos,
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.ContentBlock!.Length, actualRecord.ContentLength);

        TestUtils.AssertRequestRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = RequestRecord.DefaultOrderedFields;
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
    public async Task RecordForResourceThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "resource.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync();
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
            actualRecord.IdentifiedPayloadType,
            actualRecord.IpAddress,
            actualRecord.ConcurrentTos,
            actualRecord.IsSegmented(),
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.RecordBlock!.Length, actualRecord.ContentLength);

        TestUtils.AssertResourceRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = ResourceRecord.DefaultOrderedFields;
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
    public async Task RecordForResponseThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "response.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync();
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
            actualRecord.IdentifiedPayloadType,
            actualRecord.IpAddress,
            actualRecord.ConcurrentTos,
            actualRecord.IsSegmented(),
            actualRecord.TruncatedReason,
            digestFactory);

        Assert.Equal(expectedRecord.ContentBlock!.Length, actualRecord.ContentLength);

        TestUtils.AssertResponseRecord(
            actualRecord,
            version,
            isWithoutBlockDigest);

        var orderedFields = ResponseRecord.DefaultOrderedFields;
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
    public async Task RecordForRevisitOfIdenticalThatIsUncompressed(string version, bool isWithoutBlockDigest)
    {
        var parser = new WarcParser();
        var path = CreatePath(
            "revisit_identical.warc",
            version,
            isWithoutBlockDigest);
        var parseLog = new CustomParseLog();

        var records = await parser.Parse(path, parseLog).ToListAsync();
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

        TestUtils.AssertRevisitRecordForIdentical(
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
        // creating a record with a Content-Length of zero will cause a non-null Content-Type to be
        // null. Thus, the Content-Type for this record is removed.
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

        var records = await parser.Parse(path, parseLog).ToListAsync();
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

        TestUtils.AssertRevisitRecordForUnmodified(
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

        var records = await parser.Parse(path, parseLog).ToListAsync();
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

        TestUtils.AssertWarcinfoRecord(actualRecord, isWithoutBlockDigest);

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

    private static List<string> AssertHeaderAndToString(WarcProtocol.Record record, IEnumerable<string> defaultOrderedFields)
    {
        var expectedHeader = record.GetHeader();
        var expectedHeaderTokens = expectedHeader.Split(WarcParser.CrLf, StringSplitOptions.RemoveEmptyEntries);
        var actualFields = new List<string>(defaultOrderedFields);

        var headerDeclarationAndFieldCount = actualFields.Count + 1;
        Assert.Equal(expectedHeaderTokens.Length, headerDeclarationAndFieldCount);

        var actualHeader = record.GetHeader(actualFields);
        var actualHeaderTokens = actualHeader.Split(WarcParser.CrLf, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(headerDeclarationAndFieldCount, actualHeaderTokens.Length);

        return actualFields;
    }

    private static string CompressFile(string path)
    {
        using var inputStream = File.OpenRead(path);
        var tempPath = TestUtils.CreateTempFile(FileExtensionForCompressed);
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