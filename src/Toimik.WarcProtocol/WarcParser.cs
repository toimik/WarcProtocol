/*
 * Copyright 2021-2022 nurhafiz@hotmail.sg
 *
 * Licensed under the Apache License, version 2.0 (the "License");
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
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a parser for WARC files that are formatted according to version 1.1 and 1.0.
/// </summary>
/// <remarks>
/// A WARC file consists of record(s) of several predefined types.
/// </remarks>
public sealed class WarcParser
{
    internal const int CarriageReturn = 0xD;

    internal const int LineFeed = 0xA;

    internal static readonly string CrLf = "\r\n";

    private static readonly ISet<string> MandatoryHeaderFields = new HashSet<string>
    {
        Record.FieldForDate,
        Record.FieldForRecordId,
        Record.FieldForType,
        Record.FieldForContentLength,
    };

    private static readonly ISet<string> SupportedVersions = new HashSet<string>
    {
        "1.0",
        "1.1",
    };

    public WarcParser(RecordFactory? recordFactory = null, CompressionStreamFactory? compressionStreamFactory = null)
    {
        RecordFactory = recordFactory ?? new RecordFactory();
        CompressionStreamFactory = compressionStreamFactory ?? new CompressionStreamFactory();
    }

    public CompressionStreamFactory CompressionStreamFactory { get; }

    public RecordFactory RecordFactory { get; }

    /// <summary>
    /// Parses a file containing WARC record(s).
    /// </summary>
    /// <param name="path">
    /// Path to the file. If it ends with a '.gz' extension, it is assumed that the file is
    /// compressed with gzip either in its entirety or per record.
    /// </param>
    /// <param name="parseLog">
    /// If <c>null</c>, parsing terminates immediately if an exception is thrown. Otherwise,
    /// parsing continues but all errors and skipped chunks are passed to this
    /// <see cref="IParseLog"/>.
    /// </param>
    /// <param name="byteOffset">
    /// Number of bytes to offset.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token to monitor for cancellation request.
    /// </param>
    /// <returns>
    /// Parsed <see cref="Record"/>(s).
    /// </returns>
    /// <remarks>
    /// A malformed warc file may have a Content-Length that does not match the content block's
    /// length; The actual length may be shorter or longer. This introduces a complication if
    /// the file has several uncompressed records or records that are compressed as a whole.
    /// <para>
    /// This is because a content block that is shorter than its Content-Length causes the next
    /// record's data, if any, to be appended to the current record's content block, thus
    /// corrupting it. In turn, parsing the remainder of the next record, if any, causes an
    /// exception, which is suppressed if <see cref="IParseLog"/> is specified. Consequently,
    /// what remains of the next record is entirely discarded before parsing continues from the
    /// subsequent record, if any.
    /// </para>
    /// <para>
    /// Similarly, a content block that is longer than its Content-Length corrupts the current
    /// record's content block. This is due to a truncation that discards all remaining data up
    /// to the beginning of the next record. The difference is that the next record, if any, is
    /// unaffected.
    /// </para>
    /// <para>
    /// What this means is that <see cref="IParseLog"/> is useful only when used to parse files
    /// containing one (compressed / uncompressed) record or multiple records that are
    /// individually compressed.
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<Record> Parse(
        string path,
        IParseLog? parseLog = null,
        long byteOffset = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var stream = File.OpenRead(path);
        var isCompressed = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        await foreach (Record record in Parse(
            stream,
            isCompressed,
            parseLog,
            byteOffset,
            cancellationToken).ConfigureAwait(false))
        {
            yield return record;
        }
    }

    /// <summary>
    /// Similar to <see cref="Parse(string, IParseLog, long, CancellationToken)"/> except that a
    /// stream and an indication of whether it is compressed is passed.
    /// </summary>
    /// <param name="stream">
    /// A <see cref="Stream"/> representing the content of a WARC file.
    /// </param>
    /// <param name="isCompressed">
    /// An indication of whether <paramref name="stream"/> is GZip-ed compressed.
    /// </param>
    /// <param name="parseLog">
    /// If <c>null</c>, parsing terminates immediately if an exception is thrown. Otherwise,
    /// parsing continues but all errors and skipped chunks are passed to this
    /// <see cref="IParseLog"/>.
    /// </param>
    /// <param name="byteOffset">
    /// Number of bytes to offset.
    /// </param>
    /// <param name="cancellationToken">
    /// Optional token to monitor for cancellation request.
    /// </param>
    /// <returns>
    /// Parsed <see cref="Record"/>(s).
    /// </returns>
    /// <remarks>
    /// The stream is kept opened after processing.
    /// <para>
    /// Refer to <see cref="Parse(string, IParseLog, long, CancellationToken)"/> for additional
    /// remarks.
    /// </para>
    /// </remarks>
    public async IAsyncEnumerable<Record> Parse(
        Stream stream,
        bool isCompressed,
        IParseLog? parseLog = null,
        long byteOffset = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LineReader lineReader;
        Stream? decompressStream = null;
        if (!isCompressed)
        {
            lineReader = new(stream, cancellationToken);
        }
        else
        {
            /* A WARC file can be compressed either in its entirety or per record. If it is the
             * latter, the default decompress stream throws an exception after reading each
             * record. Therefore, the parsing is wrapped inside a loop where the data is read
             * until the end of file (EOF).
             *
             * In either cases, a format exception may be thrown due to malformed content.
             * Unless otherwise stated, parsing continues until EOF.
             */

            decompressStream = CompressionStreamFactory.CreateDecompressStream(stream);
            lineReader = new(decompressStream, cancellationToken);
        }

        await lineReader.Offset(byteOffset).ConfigureAwait(false);

        try
        {
            do
            {
                Record? record = null;
                try
                {
                    record = await Parse(lineReader, parseLog).ConfigureAwait(false);
                    if (record == null)
                    {
                        break;
                    }
                }
                catch (FormatException ex)
                {
                    if (parseLog == null)
                    {
                        throw;
                    }

                    parseLog.ErrorEncountered(ex.Message);
                    continue;
                }

                yield return record;
            }
            while (true);
        }
        finally
        {
            decompressStream?.Close();
        }
    }

    private static string? GetAnyUndefinedMandatoryHeaderField(IDictionary<string, string> fieldToValue)
    {
        string? field = null;
        foreach (string headerField in MandatoryHeaderFields)
        {
            if (!fieldToValue.ContainsKey(headerField))
            {
                field = headerField;
                break;
            }
        }

        return field;
    }

    private static async Task<byte[]> ParseContentBlock(LineReader lineReader, int contentLength)
    {
        /* Content block starts after the second pair of crlf, of which the first is read
         * earlier. The block is as long as the value indicated by the Content-Length header and
         * may consist of a payload.
         */

        var contentBlock = new byte[contentLength];
        var readCount = 0;
        var remainder = contentLength;

        /* NOTE: Looping is required because Stream.ReadAsync(...) does not always return all
         * the characters up to the buffer's length
         */

        var hasReadAllData = remainder == 0;
        while (!hasReadAllData)
        {
            readCount = await lineReader.Stream.ReadAsync(contentBlock.AsMemory(readCount, remainder), lineReader.CancellationToken).ConfigureAwait(false);
            var isEofEncountered = readCount == 0;
            remainder -= readCount;
            hasReadAllData = remainder == 0;
            if (isEofEncountered
                || hasReadAllData)
            {
                break;
            }
        }

        var hasMissingData = remainder > 0;
        if (hasMissingData)
        {
            var line = Encoding.UTF8.GetString(contentBlock);
            var text = $"Content block is {remainder} bytes shorter than expected length ({contentLength}): {line}";
            throw new FormatException(text);
        }

        return contentBlock;
    }

    private static string ProcessRecordDeclaration(string line)
    {
        /* NOTE: Record declaration is the first line of every record that must be formatted as
         * 'WARC/<version>'
         */

        var tokens = line.Split('/');
        if (tokens.Length != 2)
        {
            var text = $"Invalid record declaration: {line}";
            throw new FormatException(text);
        }

        var version = tokens[1].Trim();
        var isSupportedVersion = SupportedVersions.Contains(version);
        if (!isSupportedVersion)
        {
            var text = $"Unsupported format version: {line}";
            throw new FormatException(text);
        }

        return version;
    }

    private static async Task<string?> ReadUntilNextRecord(LineReader lineReader, IParseLog? parseLog)
    {
        /* Move the stream's position to the next occurrence of 'WARC/' (case-insensitive) */

        var builder = new StringBuilder();
        string? line;
        while (true)
        {
            line = await lineReader.Read().ConfigureAwait(false);
            if (line == null
                || line.StartsWith("WARC/", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (line == string.Empty)
            {
                builder.Append(CrLf);
            }
            else
            {
                builder.Append(line)
                    .Append(CrLf);
            }
        }

        var chunk = builder.ToString();
        if (parseLog != null
            && chunk.Length > 0
            && !chunk.Equals($"{CrLf}{CrLf}"))
        {
            parseLog.ChunkSkipped(chunk);
        }

        return line;
    }

    private static void SetHeaderFields(Record record, IDictionary<string, string> fieldToValue)
    {
        foreach (KeyValuePair<string, string> kvp in fieldToValue)
        {
            record.Set(kvp.Key, kvp.Value);
        }
    }

    private static void ValidateMandatoryHeaderFields(IDictionary<string, string> fieldToValue)
    {
        var field = GetAnyUndefinedMandatoryHeaderField(fieldToValue);
        if (field != null)
        {
            var text = $"One of the mandatory header fields is missing: {field}";
            throw new FormatException(text);
        }
    }

    private async Task<Record?> Parse(LineReader lineReader, IParseLog? parseLog)
    {
        var line = await ReadUntilNextRecord(lineReader, parseLog).ConfigureAwait(false);
        var isEofEncountered = line == null;
        if (isEofEncountered)
        {
            return null;
        }

        Record? record;
        string? recordType = null;
        IDictionary<string, string>? fieldToValue = null;
        try
        {
            var version = ProcessRecordDeclaration(line!);

            /* As header fields can be in any order, there is no choice but to perform two
             * passes on the header because a record can only be instantiated after knowing the
             * value of WARC-Type
             */

            fieldToValue = await Utils.ParseWarcFields(lineReader).ConfigureAwait(false);
            ValidateMandatoryHeaderFields(fieldToValue);
            recordType = fieldToValue[Record.FieldForType];
            var recordId = fieldToValue[Record.FieldForRecordId];
            var date = fieldToValue[Record.FieldForDate];
            record = RecordFactory.CreateRecord(
                version,
                recordType,
                Utils.RemoveBracketsFromUri(recordId),
                DateTime.Parse(date));
            fieldToValue.Remove(Record.FieldForType);
            SetHeaderFields(record, fieldToValue);
            var contentLength = int.Parse(fieldToValue[Record.FieldForContentLength]);
            var contentBlock = await ParseContentBlock(lineReader, contentLength).ConfigureAwait(false);
            record.SetContentBlock(contentBlock);
        }
        catch (FormatException ex)
        {
            var message = ex.Message;
            if (fieldToValue != null)
            {
                if (recordType != null)
                {
                    fieldToValue.Add(Record.FieldForType, recordType);
                }

                message = $"{message}{Environment.NewLine}{Environment.NewLine}Headers:{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, fieldToValue)}";
            }

            throw new FormatException(message);
        }

        return record;
    }
}