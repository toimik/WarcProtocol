namespace Toimik.WarcProtocol.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Text;
    using ICSharpCode.SharpZipLib.GZip;
    using Xunit;

    public class WarcParserTest
    {
        private const string FileExtensionForCompressed = ".warc.gz";

        private const string FileExtensionForUncompressed = ".warc";

        private static readonly string DirectoryForInvalidRecords = $"Data{Path.DirectorySeparatorChar}Invalid{Path.DirectorySeparatorChar}";

        private static readonly string DirectoryForValidRecords = $"Data{Path.DirectorySeparatorChar}Valid{Path.DirectorySeparatorChar}";

        private readonly ISet<string> MergeFilenames = new HashSet<string>
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
        public void ExceptionDueToLongerContentLengthButIsSuppressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForInvalidRecords}short_content_block.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var record = GetFirstRecord(records);

            Assert.Null(record);
            var actualMessage = parseLog.Messages[0];
            Assert.Contains("Content block is", actualMessage);
        }

        [Fact]
        public void ExceptionDueToLongerContentLengthButIsUnsuppressed()
        {
            var parser = new WarcParser();

            var path = $"{DirectoryForInvalidRecords}short_content_block.warc";
            var records = parser.Parse(path);

            var ex = Assert.Throws<FormatException>(() => GetFirstRecord(records));
            Assert.Contains("Content block is", ex.Message);
        }

        [Fact]
        public void ExceptionDueToMalformedHeaderThatIsSuppressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForInvalidRecords}malformed_header.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            GetFirstRecord(records);

            var expectedMessages = new List<string>
            {
                // NOTE: The first two are ignored because they do not start with 'warc/'

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
        public void ExceptionDueToMalformedHeaderThatIsUnsuppressed()
        {
            var parser = new WarcParser();

            var path = $"{DirectoryForInvalidRecords}malformed_header.warc";
            var records = parser.Parse(path);

            Assert.Throws<FormatException>(() => GetFirstRecord(records));
        }

        [Fact]
        public void ExceptionDueToMissingMandatoryHeaderFieldButIsSuppressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForInvalidRecords}missing_header_field.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            GetFirstRecord(records);

            var expectedCounter = 4;
            Assert.Equal(expectedCounter, parseLog.Messages.Count);
        }

        [Fact]
        public void ExceptionDueToMissingMandatoryHeaderFieldButIsUnsuppressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForInvalidRecords}missing_header_field.warc";
            var records = parser.Parse(path);

            var exception = Assert.Throws<FormatException>(() => GetFirstRecord(records));
            Assert.Contains("One of the mandatory header fields is missing", exception.Message);
        }

        [Theory]
        [InlineData("premature_wo_trailing_newline.warc")]
        [InlineData("premature_w_trailing_newline.warc")]
        public void ExceptionDueToPrematureEndOfFileButIsSuppressed(string filename)
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForInvalidRecords}{filename}";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var record = GetFirstRecord(records);

            Assert.Null(record);
            var actualMessage = parseLog.Messages[0];
            Assert.Contains("Premature end of file", actualMessage);
        }

        [Theory]
        [InlineData("premature_wo_trailing_newline.warc")]
        [InlineData("premature_w_trailing_newline.warc")]
        public void ExceptionDueToPrematureEndOfFileButIsUnsuppressed(string filename)
        {
            var parser = new WarcParser();

            var path = $"{DirectoryForInvalidRecords}{filename}";
            var records = parser.Parse(path);

            var ex = Assert.Throws<FormatException>(() => GetFirstRecord(records));
            Assert.Contains("Premature end of file", ex.Message);
        }

        [Fact]
        public void IncorrectContentLengthThatIsUncompressed()
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

            var enumerator = records.GetEnumerator();
            for (int i = 0; i < expectedContents.Count; i++)
            {
                var expectedContent = expectedContents[i];
                enumerator.MoveNext();
                var record = (ResourceRecord)enumerator.Current;
                var actualContent = Encoding.UTF8.GetString(record.RecordBlock);
                Assert.Equal(expectedContent, actualContent);
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
        public void IndividualRecordThatIsCompressed(string filename)
        {
            string tempCompressedFile = null;
            try
            {
                var path = $"{DirectoryForValidRecords}{filename}";
                tempCompressedFile = CompressFile(path);

                TestFile(tempCompressedFile, recordCount: 1);
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
        public void MergedRecordsThatAreIndividuallyCompressedAsOneFile(bool isCustomCompressionStreamFactoryUsed)
        {
            string path = null;
            try
            {
                path = CreateTempFile(FileExtensionForCompressed);
                MergeIndividuallyCompressedRecords(path);

                var compressionStreamFactory = isCustomCompressionStreamFactoryUsed
                    ? new SharpZipCompressionStreamFactory()
                    : null;
                TestFile(
                    path,
                    MergeFilenames.Count,
                    compressionStreamFactory);
            }
            finally
            {
                DeleteFile(path);
            }
        }

        [Fact]
        public void MergedRecordsThatAreUncompressedAsOneFile()
        {
            string path = null;
            try
            {
                path = CreateTempFile(FileExtensionForUncompressed);
                MergeUncompressedRecords(path);

                TestFile(path, MergeFilenames.Count);
            }
            finally
            {
                DeleteFile(path);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MergedRecordsThatAreWhollyCompressedAsOneFile(bool isCustomCompressionStreamFactoryUsed)
        {
            string path = null;
            string tempCompressedFile = null;
            try
            {
                path = CreateTempFile(FileExtensionForCompressed);
                MergeUncompressedRecords(path);
                tempCompressedFile = CompressFile(path);

                var compressionStreamFactory = isCustomCompressionStreamFactoryUsed
                    ? new SharpZipCompressionStreamFactory()
                    : null;
                TestFile(
                    tempCompressedFile,
                    MergeFilenames.Count,
                    compressionStreamFactory);
            }
            finally
            {
                DeleteFile(tempCompressedFile);
                DeleteFile(path);
            }
        }

        [Fact]
        public void MultilineHeaderValues()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForInvalidRecords}multiline_header_values.warc";

            var records = parser.Parse(path);
            var record = (ResourceRecord)GetFirstRecord(records);

            Assert.Equal("1.0", record.Version);
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
            Assert.Equal(new Uri("urn:uuid:1a59c5c8-d806-4cdb-83aa-b21495d11063"), record.Id);
            Assert.Equal(1, record.ContentLength);
            Assert.Equal("A", Encoding.UTF8.GetString(record.RecordBlock));
        }

        [Fact]
        public void OffsetOverLimit()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForInvalidRecords}incorrect_content_length.warc";

            var records = parser.Parse(path, byteOffset: 1000);
            var exception = Assert.Throws<ArgumentException>(() => GetFirstRecord(records));

            Assert.Contains("Offset exceeds file size", exception.Message);
        }

        [Fact]
        public void OffsetUnderLimit()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForInvalidRecords}incorrect_content_length.warc";

            var records = parser.Parse(path, byteOffset: 697);
            var record = (ResourceRecord)GetFirstRecord(records);

            Assert.Equal("1.1", record.Version);
            Assert.Equal(DateTime.Parse("2001-01-01T12:34:56Z"), record.Date);
            Assert.Equal(new Uri("urn:uuid:1a59c5c8-d806-4cdb-83aa-b21495d11063"), record.Id);
            Assert.Equal("Resource", record.Type);
            Assert.Equal(6, record.ContentLength);
            Assert.Equal("foobar", Encoding.UTF8.GetString(record.RecordBlock));
        }

        [Fact]
        public void RecordForContinuationThatIsUncompressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForValidRecords}continuation.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var actualRecord = (ContinuationRecord)GetFirstRecord(records);

            var expectedRecord = new ContinuationRecord(
                actualRecord.Version,
                actualRecord.Id,
                actualRecord.Date,
                actualRecord.RecordBlock,
                actualRecord.PayloadDigest,
                actualRecord.InfoId,
                actualRecord.TargetUri,
                actualRecord.SegmentOriginId,
                actualRecord.SegmentNumber,
                actualRecord.SegmentTotalLength,
                actualRecord.TruncatedReason,
                actualRecord.DigestFactory)
            {
            };

            Assert.Equal(expectedRecord.RecordBlock.Length, actualRecord.ContentLength);
            Assert.Equal(expectedRecord.RecordBlock, actualRecord.RecordBlock);

            AssertContinuationRecord(actualRecord);

            var fields = AssertHeaderAndToString(
                actualRecord,
                ContinuationRecord.DefaultOrderedFields);

            fields.Sort();
            var expectedHeader = expectedRecord.GetHeader(fields);
            var actualHeader = actualRecord.GetHeader(fields);
            Assert.Equal(expectedHeader, actualHeader);
        }

        [Fact]
        public void RecordForConversionThatIsUncompressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForValidRecords}conversion.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var actualRecord = (ConversionRecord)GetFirstRecord(records);

            var expectedRecord = new ConversionRecord(
                actualRecord.Version,
                actualRecord.Id,
                actualRecord.Date,
                actualRecord.PayloadTypeIdentifier,
                actualRecord.RecordBlock,
                actualRecord.ContentType,
                actualRecord.InfoId,
                actualRecord.TargetUri,
                actualRecord.PayloadDigest,
                actualRecord.RefersTo,
                actualRecord.IsSegmented(),
                actualRecord.TruncatedReason,
                actualRecord.DigestFactory)
            {
            };

            Assert.Equal(expectedRecord.RecordBlock.Length, actualRecord.ContentLength);
            Assert.Null(expectedRecord.IdentifiedPayloadType);

            // NOTE: See remarks #1
            Assert.Null(actualRecord.IdentifiedPayloadType);

            AssertConversionRecord(actualRecord);

            var fields = AssertHeaderAndToString(
                actualRecord,
                ConversionRecord.DefaultOrderedFields,
                hasIgnoredIdentifiedPayloadType: true);

            fields.Sort();
            var expectedHeader = expectedRecord.GetHeader(fields);
            var actualHeader = actualRecord.GetHeader(fields);
            Assert.Equal(expectedHeader, actualHeader);
        }

        [Fact]
        public void RecordForMetadataThatIsUncompressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForValidRecords}metadata.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var actualRecord = (MetadataRecord)GetFirstRecord(records);

            var expectedRecord = new MetadataRecord(
                actualRecord.Version,
                actualRecord.Id,
                actualRecord.Date,
                actualRecord.ContentBlock,
                actualRecord.ContentType,
                actualRecord.InfoId,
                actualRecord.TargetUri,
                actualRecord.IpAddress,
                actualRecord.RefersTo,
                actualRecord.ConcurrentTos,
                actualRecord.TruncatedReason,
                actualRecord.DigestFactory)
            {
            };

            Assert.Equal(expectedRecord.ContentBlock.Length, actualRecord.ContentLength);
            Assert.Equal(expectedRecord.ContentBlock, actualRecord.ContentBlock);

            AssertMetadataRecord(actualRecord);

            var fields = AssertHeaderAndToString(
                actualRecord,
                MetadataRecord.DefaultOrderedFields);

            fields.Sort();
            var expectedHeader = expectedRecord.GetHeader(fields);
            var actualHeader = actualRecord.GetHeader(fields);
            Assert.Equal(expectedHeader, actualHeader);
        }

        [Fact]
        public void RecordForRequestThatIsUncompressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForValidRecords}request.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var actualRecord = (RequestRecord)GetFirstRecord(records);

            var expectedRecord = new RequestRecord(
                actualRecord.Version,
                actualRecord.Id,
                actualRecord.Date,
                actualRecord.PayloadTypeIdentifier,
                actualRecord.ContentBlock,
                actualRecord.ContentType,
                actualRecord.InfoId,
                actualRecord.TargetUri,
                actualRecord.PayloadDigest,
                actualRecord.IpAddress,
                actualRecord.ConcurrentTos,
                actualRecord.TruncatedReason,
                actualRecord.DigestFactory);
            {
            };

            Assert.Equal(expectedRecord.ContentBlock.Length, actualRecord.ContentLength);
            Assert.Null(expectedRecord.IdentifiedPayloadType);
            Assert.Null(actualRecord.IdentifiedPayloadType);

            AssertRequestRecord(actualRecord);

            var fields = AssertHeaderAndToString(
                actualRecord,
                RequestRecord.DefaultOrderedFields,
                hasIgnoredIdentifiedPayloadType: true);

            fields.Sort();
            var expectedHeader = expectedRecord.GetHeader(fields);
            var actualHeader = actualRecord.GetHeader(fields);
            Assert.Equal(expectedHeader, actualHeader);
        }

        [Fact]
        public void RecordForResourceThatIsUncompressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForValidRecords}resource.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var actualRecord = (ResourceRecord)GetFirstRecord(records);

            var expectedRecord = new ResourceRecord(
                actualRecord.Version,
                actualRecord.Id,
                actualRecord.Date,
                actualRecord.PayloadTypeIdentifier,
                actualRecord.RecordBlock,
                actualRecord.ContentType,
                actualRecord.InfoId,
                actualRecord.TargetUri,
                actualRecord.PayloadDigest,
                actualRecord.IpAddress,
                actualRecord.ConcurrentTos,
                actualRecord.IsSegmented(),
                actualRecord.TruncatedReason,
                actualRecord.DigestFactory)
            {
            };

            Assert.Equal(expectedRecord.RecordBlock.Length, actualRecord.ContentLength);
            Assert.Null(actualRecord.IdentifiedPayloadType);

            // NOTE: See remarks #1
            Assert.Null(actualRecord.IdentifiedPayloadType);

            AssertResourceRecord(actualRecord);

            var fields = AssertHeaderAndToString(
                actualRecord,
                ResourceRecord.DefaultOrderedFields,
                hasIgnoredIdentifiedPayloadType: true);

            fields.Sort();
            var expectedHeader = expectedRecord.GetHeader(fields);
            var actualHeader = actualRecord.GetHeader(fields);
            Assert.Equal(expectedHeader, actualHeader);
        }

        [Fact]
        public void RecordForResponseThatIsUncompressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForValidRecords}response.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var actualRecord = (ResponseRecord)GetFirstRecord(records);

            var expectedRecord = new ResponseRecord(
                actualRecord.Version,
                actualRecord.Id,
                actualRecord.Date,
                actualRecord.PayloadTypeIdentifier,
                actualRecord.ContentBlock,
                actualRecord.ContentType,
                actualRecord.InfoId,
                actualRecord.TargetUri,
                actualRecord.PayloadDigest,
                actualRecord.IpAddress,
                actualRecord.ConcurrentTos,
                actualRecord.IsSegmented(),
                actualRecord.TruncatedReason,
                actualRecord.DigestFactory)
            {
            };

            Assert.Equal(expectedRecord.ContentBlock.Length, actualRecord.ContentLength);
            Assert.Null(expectedRecord.IdentifiedPayloadType);

            // NOTE: See remarks #1
            Assert.Null(actualRecord.IdentifiedPayloadType);

            AssertResponseRecord(actualRecord);

            var fields = AssertHeaderAndToString(
                actualRecord,
                ResponseRecord.DefaultOrderedFields,
                hasIgnoredIdentifiedPayloadType: true);

            fields.Sort();
            var expectedHeader = expectedRecord.GetHeader(fields);
            var actualHeader = actualRecord.GetHeader(fields);
            Assert.Equal(expectedHeader, actualHeader);
        }

        [Fact]
        public void RecordForRevisitOfIdenticalThatIsUncompressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForValidRecords}revisit_identical.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var actualRecord = (RevisitRecord)GetFirstRecord(records);

            var expectedRecord = new RevisitRecord(
                actualRecord.Version,
                actualRecord.Id,
                actualRecord.Date,
                actualRecord.RecordBlock,
                actualRecord.ContentType,
                actualRecord.InfoId,
                actualRecord.TargetUri,
                actualRecord.Profile,
                actualRecord.IpAddress,
                actualRecord.RefersTo,
                actualRecord.RefersToDate,
                actualRecord.RefersToTargetUri,
                actualRecord.ConcurrentTos,
                actualRecord.TruncatedReason,
                actualRecord.DigestFactory)
            {
            };

            Assert.Equal(expectedRecord.RecordBlock.Length, actualRecord.ContentLength);
            Assert.Equal(expectedRecord.RecordBlock, actualRecord.RecordBlock);

            AssertRevisitRecordForIdentical(actualRecord);

            var fields = AssertHeaderAndToString(
                actualRecord,
                RevisitRecord.DefaultOrderedFields);

            fields.Sort();
            var expectedHeader = expectedRecord.GetHeader(fields);

            // NOTE: Parsing warc files will preserve the value of Content-Type, if any. However,
            // creating a record with a Content-Length of zero will cause a non-null Content-Type to
            // be null. Thus, the Content-Type for this record is removed.
            fields.Remove(RevisitRecord.FieldForContentType);

            var actualHeader = actualRecord.GetHeader(fields);
            Assert.Equal(expectedHeader, actualHeader);
        }

        [Fact]
        public void RecordForRevisitOfUnmodifiedThatIsUncompressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForValidRecords}revisit_unmodified.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var actualRecord = (RevisitRecord)GetFirstRecord(records);

            var expectedRecord = new RevisitRecord(
                actualRecord.Version,
                actualRecord.Id,
                actualRecord.Date,
                actualRecord.RecordBlock,
                actualRecord.ContentType,
                actualRecord.InfoId,
                actualRecord.TargetUri,
                actualRecord.Profile,
                actualRecord.IpAddress,
                actualRecord.RefersTo,
                actualRecord.RefersToDate,
                actualRecord.RefersToTargetUri,
                actualRecord.ConcurrentTos,
                actualRecord.TruncatedReason,
                actualRecord.DigestFactory)
            {
            };

            Assert.Equal(expectedRecord.RecordBlock.Length, actualRecord.ContentLength);
            Assert.Equal(expectedRecord.RecordBlock, actualRecord.RecordBlock);

            AssertRevisitRecordForUnmodified(actualRecord);

            var fields = AssertHeaderAndToString(
                actualRecord,
                RevisitRecord.DefaultOrderedFields);

            fields.Sort();
            var expectedHeader = expectedRecord.GetHeader(fields);
            var actualHeader = actualRecord.GetHeader(fields);
            Assert.Equal(expectedHeader, actualHeader);
        }

        [Fact]
        public void RecordForWarcinfoThatIsUncompressed()
        {
            var parser = new WarcParser();
            var path = $"{DirectoryForValidRecords}warcinfo.warc";
            var parseLog = new CustomParseLog();

            var records = parser.Parse(path, parseLog);
            var actualRecord = (WarcinfoRecord)GetFirstRecord(records);

            var expectedRecord = new WarcinfoRecord(
                actualRecord.Version,
                actualRecord.Id,
                actualRecord.Date,
                actualRecord.ContentBlock,
                actualRecord.ContentType,
                actualRecord.Filename,
                actualRecord.TruncatedReason,
                actualRecord.DigestFactory)
            {
            };

            Assert.Equal(expectedRecord.ContentBlock.Length, actualRecord.ContentLength);
            Assert.Equal(expectedRecord.ContentBlock, actualRecord.ContentBlock);

            AssertWarcinfoRecord(actualRecord);

            var fields = AssertHeaderAndToString(
                actualRecord,
                WarcinfoRecord.DefaultOrderedFields);

            fields.Sort();
            var expectedHeader = expectedRecord.GetHeader(fields);
            var actualHeader = actualRecord.GetHeader(fields);
            Assert.Equal(expectedHeader, actualHeader);
        }

        private static void AssertContinuationRecord(ContinuationRecord record)
        {
            Assert.Equal("1.1", record.Version);
            Assert.Equal("sha1:C767A59C4DBC7430855F7BF1468D495376E92C3D", record.BlockDigest);
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
            Assert.Equal("sha1:CB927B7C7DE7DA663A68973E0C034FFBE6A98BF4", record.PayloadDigest);
            Assert.Equal(new Uri("urn:uuid:6a59c5c8-d806-4cdb-83aa-b21495d11063"), record.Id);
            Assert.Equal(2, record.SegmentNumber);
            Assert.Equal(new Uri("urn:uuid:a6ddea17-518c-44d7-8d34-77f8ef0d0890"), record.SegmentOriginId);
            Assert.Equal(217, record.SegmentTotalLength);
            Assert.Equal(new Uri("dns://example.com"), record.TargetUri);
            Assert.Equal("Continuation", record.Type);
            Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
            Assert.Equal(83, record.ContentLength);
        }

        private static void AssertConversionRecord(ConversionRecord record)
        {
            Assert.Equal("1.1", record.Version);
            Assert.Equal("sha1:14688051DB31569CB48D9C36C818B593A5228916", record.BlockDigest);
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
            Assert.Equal(new Uri("urn:uuid:07d18ee8-5a5e-43e1-9ff9-320a994141fd"), record.Id);
            Assert.Equal(new Uri("urn:uuid:a6ddea17-518c-44d7-8d34-77f8ef0d0890"), record.RefersTo);
            Assert.Equal(1, record.SegmentNumber);
            Assert.Equal(new Uri("file://var/www/htdoc/robots.txt"), record.TargetUri);
            Assert.Equal("Conversion", record.Type);
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

        private static void AssertMetadataRecord(MetadataRecord record)
        {
            Assert.Equal("1.1", record.Version);
            Assert.Equal("sha1:25EEDCA9AC3593DBDB2E3B5D69219231A8824257", record.BlockDigest);
            Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:25809bd7-a7e7-4a97-81b5-d7d9747f3eaf")));
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
            Assert.Equal(new Uri("urn:uuid:90c6fd91-f744-4818-95d1-693f933d2214"), record.Id);
            Assert.Equal(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221"), record.RefersTo);
            Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
            Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
            Assert.Equal("Metadata", record.Type);
            Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
            Assert.Equal(62, record.ContentLength);
            var actualContentType = new ContentTypeIdentifier().Identify(record);
            Assert.Equal(record.ContentType, actualContentType);
        }

        private static void AssertRequestRecord(RequestRecord record)
        {
            Assert.Equal("1.1", record.Version);
            Assert.Equal("sha1:85FFD42680A33B720124A4D29E6CB4F8C2798C92", record.BlockDigest);
            Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221")));
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
            Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
            Assert.Equal("sha1:DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", record.PayloadDigest);
            Assert.Equal(new Uri("urn:uuid:672baea5-b4cd-49e3-9ce7-6cdf93513d54"), record.Id);
            Assert.Equal("Request", record.Type);
            Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
            Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
            Assert.Equal(190, record.ContentLength);
            var actualContentType = new ContentTypeIdentifier().Identify(record);
            Assert.Equal(record.ContentType, actualContentType);
        }

        private static void AssertResourceRecord(ResourceRecord record)
        {
            Assert.Equal("1.1", record.Version);
            Assert.Equal("sha1:231AD621E019B0108B5886A3ABFDDB6D8F9477D2", record.BlockDigest);
            Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:25809bd7-a7e7-4a97-81b5-d7d9747f3eaf")));
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
            Assert.Equal(IPAddress.Parse("127.0.0.1"), record.IpAddress);
            Assert.Equal("sha1:CB927B7C7DE7DA663A68973E0C034FFBE6A98BF4", record.PayloadDigest);
            Assert.Equal(new Uri("urn:uuid:a6ddea17-518c-44d7-8d34-77f8ef0d0890"), record.Id);
            Assert.Equal(new Uri("dns://example.com"), record.TargetUri);
            Assert.Equal("Resource", record.Type);
            Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
            Assert.Equal(174, record.ContentLength);
            Assert.Equal(1, record.SegmentNumber);
            Assert.Equal("text/dns", record.ContentType);
            var actualContentType = new ContentTypeIdentifier().Identify(record);
            Assert.Equal("text/dns", actualContentType);
        }

        private static void AssertResponseRecord(ResponseRecord record)
        {
            Assert.Equal("1.1", record.Version);
            Assert.Equal("sha1:335680BF19511379DB8BAB0C3F3D92318751C0C4", record.BlockDigest);
            Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:672baea5-b4cd-49e3-9ce7-6cdf93513d54")));
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
            Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
            Assert.Equal("sha1:D25B43EB7F3483D5C2D8891C195A7F957EF9159C", record.PayloadDigest);
            Assert.Equal(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221"), record.Id);
            Assert.Equal(1, record.SegmentNumber);
            Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
            Assert.Equal("Response", record.Type);
            Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
            Assert.Equal(1679, record.ContentLength);
            var actualContentType = new ContentTypeIdentifier().Identify(record);
            Assert.Equal(record.ContentType, actualContentType);
        }

        private static void AssertRevisitRecordForIdentical(RevisitRecord record)
        {
            Assert.Equal("1.1", record.Version);
            Assert.Equal("sha1:DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", record.BlockDigest);
            Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221")));
            Assert.Equal(DateTime.Parse("2001-02-01T01:23:45Z"), record.Date);
            Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
            Assert.Equal(new Uri("http://netpreserve.org/warc/1.1/revisit/identical-payload-digest"), record.Profile);
            Assert.Equal(new Uri("urn:uuid:c48e2242-219c-45b1-b0c3-ecc6b784e4ed"), record.Id);
            Assert.Equal(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221"), record.RefersTo);
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.RefersToDate);
            Assert.Equal(new Uri("http://www.example.com"), record.RefersToTargetUri);
            Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
            Assert.Equal("Revisit", record.Type);
            Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
            Assert.Equal(0, record.ContentLength);
            Assert.Equal("irrelevant/but-still-parsed-for-preservation", record.ContentType);
            var actualContentType = new ContentTypeIdentifier().Identify(record);
            Assert.Equal("application/octet-stream", actualContentType);
        }

        private static void AssertRevisitRecordForUnmodified(RevisitRecord record)
        {
            Assert.Equal("1.1", record.Version);
            Assert.Equal("sha1:241E0040D1186D236DFC2DDCC80BD712DE038268", record.BlockDigest);
            Assert.True(record.ConcurrentTos.Contains(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221")));
            Assert.Equal(DateTime.Parse("2002-03-01T03:45:06Z"), record.Date);
            Assert.Equal(IPAddress.Parse("1.23.45.67"), record.IpAddress);
            Assert.Equal(new Uri("http://netpreserve.org/warc/1.1/revisit/server-not-modified"), record.Profile);
            Assert.Equal(new Uri("urn:uuid:e8381eac-7fdf-4b53-ad69-6943472920f9"), record.Id);
            Assert.Equal(new Uri("urn:uuid:17bd27ae-fb71-4b60-a7a2-d7d42cfed221"), record.RefersTo);
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.RefersToDate);
            Assert.Equal(new Uri("http://www.example.com"), record.RefersToTargetUri);
            Assert.Equal(new Uri("http://www.example.com"), record.TargetUri);
            Assert.Equal("Revisit", record.Type);
            Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.InfoId);
            Assert.Equal(198, record.ContentLength);
            Assert.Equal("message/http", record.ContentType);
            var actualContentType = new ContentTypeIdentifier().Identify(record);
            Assert.Equal("application/octet-stream", actualContentType);
        }

        private static void AssertWarcinfoRecord(WarcinfoRecord record)
        {
            Assert.Equal("1.1", record.Version);
            Assert.Equal("sha1:11C37A02E327EDE38A916B7153DE2CC6DC7876EB", record.BlockDigest);
            Assert.Equal(DateTime.Parse("2000-01-01T12:34:56Z"), record.Date);
            Assert.Equal("warcinfo.warc", record.Filename);
            Assert.Equal(new Uri("urn:uuid:b92e8444-34cf-472f-a86e-07b7845ecc05"), record.Id);
            Assert.Equal("Warcinfo", record.Type);
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

        [ExcludeFromCodeCoverage]
        private static string CreateTempFile(string fileExtension)
        {
            string path;
            string tempFilename = null;
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
        private static void DeleteFile(string path)
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

        private static WarcProtocol.Record GetFirstRecord(IEnumerable<WarcProtocol.Record> records)
        {
            var enumerator = records.GetEnumerator();
            enumerator.MoveNext();
            var record = enumerator.Current;
            return record;
        }

        private static string RenameFileExtension(string path, string extension)
        {
            var slashIndex = path.LastIndexOf(Path.DirectorySeparatorChar);
            var periodIndex = path.LastIndexOf(".");
            var length = periodIndex - slashIndex;
            var filename = path.Substring(slashIndex, length);
            var directory = path.Substring(0, slashIndex);
            var newPath = $"{directory}{filename}{extension}";
            return newPath;
        }

        private static void TestFile(
            string path,
            int recordCount,
            CompressionStreamFactory compressionStreamFactory = null)
        {
            var recordCounter = 0;
            var parser = new WarcParser(compressionStreamFactory: compressionStreamFactory);
            foreach (WarcProtocol.Record record in parser.Parse(path))
            {
                switch (record.Type.ToLower())
                {
                    case "continuation":
                        AssertContinuationRecord((ContinuationRecord)record);
                        recordCounter++;
                        break;

                    case "conversion":
                        var conversionRecord = (ConversionRecord)record;

                        // NOTE: See remarks #1
                        Assert.Null(conversionRecord.IdentifiedPayloadType);

                        AssertConversionRecord(conversionRecord);
                        recordCounter++;
                        break;

                    case "metadata":
                        AssertMetadataRecord((MetadataRecord)record);
                        recordCounter++;
                        break;

                    case "request":
                        var requestRecord = (RequestRecord)record;
                        Assert.Null(requestRecord.IdentifiedPayloadType);
                        AssertRequestRecord(requestRecord);
                        recordCounter++;
                        break;

                    case "resource":
                        AssertResourceRecord((ResourceRecord)record);
                        recordCounter++;
                        break;

                    case "response":
                        var responseRecord = (ResponseRecord)record;

                        // NOTE: See remarks #1
                        Assert.Null(responseRecord.IdentifiedPayloadType);

                        AssertResponseRecord(responseRecord);
                        recordCounter++;
                        break;

                    case "revisit":
                        var revisitRecord = (RevisitRecord)record;
                        if (revisitRecord.ContentLength == 0)
                        {
                            AssertRevisitRecordForIdentical(revisitRecord);
                        }
                        else
                        {
                            AssertRevisitRecordForUnmodified(revisitRecord);
                        }

                        recordCounter++;
                        break;

                    case "warcinfo":
                        AssertWarcinfoRecord((WarcinfoRecord)record);
                        recordCounter++;
                        break;
                }
            }

            Assert.Equal(recordCount, recordCounter);
        }

        private void MergeIndividuallyCompressedRecords(string path)
        {
            using var outputStream = File.OpenWrite(path);
            using var compressedStream = new GZipStream(outputStream, CompressionMode.Compress);
            foreach (string filename in MergeFilenames)
            {
                var tempPath = $"{DirectoryForValidRecords}{filename}";
                using var inputStream = File.OpenRead(tempPath);
                inputStream.CopyTo(compressedStream);
            }
        }

        private void MergeUncompressedRecords(string path)
        {
            using var outputStream = File.OpenWrite(path);
            foreach (string filename in MergeFilenames)
            {
                var tempPath = $"{DirectoryForValidRecords}{filename}";
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
}

// NOTE: (Remarks #1) Although the value is specified, the parser ignores it because the value must
// be independently identified using PayloadTypeIdentifier.Identify(...), which is not yet
// implemented