/*
 * Copyright 2023 https://github.com/acidus99
 * Copyright 2023 nurhafiz@hotmail.sg
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text;

/// <summary>
/// Represents a writer for WARC files that are formatted according to version 1.1 and 1.0.
/// </summary>
public class WarcWriter : IDisposable
{
    // The file to be written to
    private readonly FileStream fout;

    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WarcWriter"/> class.
    /// </summary>
    /// <param name="filePath">The full path of the WARC file to create. If the filename has a .gz extension, we will use per-record GZIP compression.</param>
    /// <param name="isForcedCompression">Use per-record GZIP compression, regardless of the WARC's file extenion.</param>
    /// <exception cref="ArgumentNullException">If the path to the WARC file is null or empty.</exception>
    /// <remarks>Validates output WARC can be written and sets up needed variables.</remarks>
    public WarcWriter(string filePath, bool isForcedCompression = false)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            // FIXME: Add a test case
            throw new ArgumentNullException(nameof(filePath), "Destination path and filename for the WARC must be specified");
        }

        var info = new FileInfo(filePath);

        // Ensures that the path exists
        if (!Directory.Exists(info.DirectoryName))
        {
            // FIXME: Add a test case
            throw new ArgumentException("Path to output WARC doesn't exist.", nameof(filePath));
        }

        Filepath = filePath;
        fout = new FileStream(Filepath, FileMode.Create);

        // Checks whether per-record compression based on *.gz file extension is used
        if (info.Extension == ".gz")
        {
            IsCompressed = true;
        }
        else if (isForcedCompression)
        {
            // FIXME: Add a test case
            IsCompressed = true;
        }
        else
        {
            IsCompressed = false;
        }
    }

    /// <summary>
    /// Gets, for this instance, the full path of the WARC file to be written to.
    /// </summary>
    public string Filepath { get; private set; }

    /// <summary>
    /// Gets, for this instance, an indication of whether per-record GZIP compression is used.
    /// </summary>
    /// <remarks>This is controlled by the file extension of the WARC or via the
    /// <c>isForcedComparession</c> parameter that is passed to the constructor.</remarks>
    public bool IsCompressed { get; private set; }

    /// <summary>
    /// Gets, for this instance, the current size of the WARC.
    /// </summary>
    /// <remarks>This is useful to know when splitting a large record into multiple smaller WARCs.</remarks>
    // FIXME: Add a test case
    public long Length => fout.Length;

    /// <summary>
    /// Closes this instance.
    /// </summary>
    // FIXME: Add a test case
    public void Close() => fout.Close();

    public void Dispose()
    {
        Dispose(isDisposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Writes the specified <paramref name="record"/> to the WARC output.
    /// </summary>
    /// <param name="record">A <see cref="Record"/>.</param>
    /// <remarks>Per-record compression is used, if applicable.</remarks>
    public void WriteRecord(Record record)
    {
        if (IsCompressed)
        {
            fout.Write(CompressRecord(record));
        }
        else
        {
            WriteRecordToStream(record, fout);
        }
    }

    [ExcludeFromCodeCoverage]
    protected virtual void Dispose(bool isDisposing)
    {
        if (isDisposed)
        {
            return;
        }

        if (isDisposing)
        {
            // Ensures that the WARC file stream is disposed
            ((IDisposable)fout).Dispose();
        }

        isDisposed = true;
    }

    /// <summary>
    /// Compresses the specified <paramref name="record"/> to a gzipped byte array.
    /// </summary>
    /// <param name="record">A <see cref="Record"/>.</param>
    /// <returns>gzipped byte array.</returns>
    private static byte[] CompressRecord(Record record)
    {
        using var memoryStream = new MemoryStream();
        using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            WriteRecordToStream(record, gzipStream);
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Outputs the specified <paramref name="record"/> to the specified <paramref name="stream"/>.
    /// </summary>
    /// <param name="record">A <see cref="Record"/>.</param>
    /// <param name="stream">A <see cref="Stream"/>.</param>
    /// <remarks>Handles the headers and optional block.</remarks>
    private static void WriteRecordToStream(Record record, Stream stream)
    {
        // Writes the header
        var header = record.GetHeader();
        stream.Write(Encoding.UTF8.GetBytes(header));
        stream.WriteByte(WarcParser.CarriageReturn);
        stream.WriteByte(WarcParser.LineFeed);

        // Writes the block, if any
        byte[]? blockBytes = record.GetBlockBytes();
        if (blockBytes != null)
        {
            stream.Write(blockBytes);
        }

        // Delimits a record with exactly two CRLFs
        stream.WriteByte(WarcParser.CarriageReturn);
        stream.WriteByte(WarcParser.LineFeed);
        stream.WriteByte(WarcParser.CarriageReturn);
        stream.WriteByte(WarcParser.LineFeed);
    }
}