namespace Toimik.WarcProtocol.Tests;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class WarcWriterTest
{
    private static readonly string DirectoryForValid1Point1Records = $"Data{Path.DirectorySeparatorChar}Valid{Path.DirectorySeparatorChar}1.1{Path.DirectorySeparatorChar}";

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
    public async Task OutputCompressedWarcMatchesInputWarc(string filename)
        => await OutputWarcMatchesInputWarc(filename, ".warc.gz");

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
    public async Task OutputUncompressedWarcMatchesInputWarc(string filename)
        => await OutputWarcMatchesInputWarc(filename, ".warc");

    // This tests how WarcWriter writes records (either uncompressed, or with
    // per-record compression) by round-tripping data from a known good source WARC files.
    // Specifically:
    // - reads source WARC through the parser to get records
    // - write those records in order to a temp, output WARC file
    // - Read back in records from output WARC to validate total record count and order of record types
    // - uses WarcParserTest's TestFile method to validate output WARC against source of truth
    private static async Task OutputWarcMatchesInputWarc(string sourceFilename, string outputExtension)
    {
        string? tempOutputWarc = null;
        try
        {
            var sourceWarc = $"{DirectoryForValid1Point1Records}{sourceFilename}";
            var outputWarc = TestUtils.CreateTempFile(outputExtension);

            List<string> sourceRecordTypes = new List<string>();

            using (var warcWriter = new WarcWriter(outputWarc))
            {
                var sourceParser = new WarcParser();
                var records = sourceParser.Parse(sourceWarc);
                await foreach (var record in records)
                {
                    // write each record from source into output WARC.
                    warcWriter.WriteRecord(record);

                    // also store the record type into a list so we can validate count and type order
                    sourceRecordTypes.Add(record.Type);
                }
            }

            // now read in the output WARC using a newly inited parser
            var outputParser = new WarcParser();
            var outputRecords = await outputParser.Parse(outputWarc).ToListAsync().ConfigureAwait(false);

            // do we have the same count, and are the record types in the same order?
            Assert.Equal(sourceRecordTypes.Count, outputRecords.Count);
            for (int i = 0; i < sourceRecordTypes.Count; i++)
            {
                // validate ordering of types
                Assert.Equal(sourceRecordTypes[i], outputRecords[i].Type);
            }

            // ensure the output warc is validates against source of truth in WarcParserTest
            await TestUtils.TestFile(outputWarc, sourceRecordTypes.Count);
        }
        finally
        {
            TestUtils.DeleteFile(tempOutputWarc);
        }
    }
}