namespace Toimik.WarcProtocol.Tests;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;

public class TestUtils
{
    [ExcludeFromCodeCoverage]
    private TestUtils()
    {
    }

    public static void AssertContinuationRecord(
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

    public static void AssertConversionRecord(
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

    public static void AssertMetadataRecord(
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

    public static void AssertRequestRecord(
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

    public static void AssertResourceRecord(
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

    public static void AssertResponseRecord(
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

    public static void AssertRevisitRecordForIdentical(
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

    public static void AssertRevisitRecordForUnmodified(
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

    public static void AssertWarcinfoRecord(WarcinfoRecord record, bool isWithoutBlockDigest = false)
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

    [ExcludeFromCodeCoverage]
    public static string CreateTempFile(string fileExtension)
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
    public static void DeleteFile(string? path)
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

    public static async Task TestFile(
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
}