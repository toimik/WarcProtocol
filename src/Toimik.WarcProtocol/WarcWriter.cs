namespace Toimik.WarcProtocol;

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

/// <summary>
/// Writes Records to a WARC file. Supports per-record compression.
/// </summary>
public class WarcWriter : IDisposable
{
    /// <summary>
    /// The underling file we will write to.
    /// </summary>
    private FileStream fout;

    /// <summary>
    /// Initializes a new instance of the <see cref="WarcWriter"/> class.
    /// Validates output WARC can be written and sets up needed variables.
    /// </summary>
    /// <param name="filepath">The full path of the WARC file to create. If the filename has a .gz extension, we will use per-record GZIP compression.</param>
    /// <param name="forceCompressed">Use per-record GZIP compression, regardless of the WARC's file extenion.</param>
    /// <exception cref="ArgumentNullException">If the path to the WARC file is null or empty.</exception>
    /// <exception cref="ArgumentException">If the WARC file already exists, or if the directory structure to the WARC doesn't exist.</exception>
    public WarcWriter(string filepath, bool forceCompressed = false)
    {
        if (string.IsNullOrEmpty(filepath))
        {
            throw new ArgumentNullException("You must provide destination path and filename for the WARC", nameof(filepath));
        }

        var info = new FileInfo(filepath);

        // ensure the file doesn't already exist
        if (File.Exists(info.FullName))
        {
            throw new ArgumentException("Supplied WARC file already exists.", nameof(filepath));
        }

        // ensure the path exists
        if (!Directory.Exists(info.DirectoryName))
        {
            throw new ArgumentException("Path to output WARC doesn't exist.", nameof(filepath));
        }

        Filepath = filepath;
        fout = new FileStream(Filepath, FileMode.CreateNew);

        // check if we are using per-record compression based on *.gz file extension
        if (info.Extension == ".gz")
        {
            IsCompressed = true;
        }
        else if (forceCompressed)
        {
            IsCompressed = true;
        }
        else
        {
            IsCompressed = false;
        }
    }

    /// <summary>
    /// The full path of the WARC file to which we are writing.
    /// </summary>
    public string Filepath { get; private set; }

    /// <summary>
    /// Are we using per-record GZIP compression?
    /// This is controlled by the file extension of the WARC or via the
    /// forceCompression option in the constructor.
    /// </summary>
    public bool IsCompressed { get; private set; }

    /// <summary>
    /// Closes the WARC output
    /// </summary>
    public void Close()
        => fout.Close();

    /// <summary>
    /// Ensures we properly dispose of the WARC file stream.
    /// </summary>
    public void Dispose()
    {
        ((IDisposable)fout).Dispose();
    }

    /// <summary>
    /// Writes a record to the WARC output, applying per-record compression if appropriate.
    /// </summary>
    /// <param name="record">The record to write to the WARC.</param>
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

    /// <summary>
    /// Compresses a record to a gzipped byte array.
    /// </summary>
    /// <param name="record">record to compress.</param>
    /// <returns>gzipped byte array</returns>
    private byte[] CompressRecord(Record record)
    {
        using (var newStream = new MemoryStream())
        {
            using (GZipStream compressed = new GZipStream(newStream, CompressionMode.Compress ))
            {
                WriteRecordToStream(record, compressed);
            }

            return newStream.ToArray();
        }
    }

    /// <summary>
    /// Outputs a record to a stream. Handles the headers and optional block.
    /// </summary>
    /// <param name="record">the record to write.</param>
    /// <param name="stream">the output stream.</param>
    private void WriteRecordToStream(Record record, Stream stream)
    {
        // write the header
        var header = record.GetHeader();
        stream.Write(Encoding.UTF8.GetBytes(header));
        stream.WriteByte(WarcParser.CarriageReturn);
        stream.WriteByte(WarcParser.LineFeed);

        // write block if available
        byte[]? blockBytes = GetBlockBytes(record);
        if (blockBytes != null)
        {
            stream.Write(blockBytes);
        }

        // Always two CRLFs between records
        stream.WriteByte(WarcParser.CarriageReturn);
        stream.WriteByte(WarcParser.LineFeed);
        stream.WriteByte(WarcParser.CarriageReturn);
        stream.WriteByte(WarcParser.LineFeed);
    }

    /// <summary>
    /// For a given record, get the bytes which make up the record block.
    /// </summary>
    /// <param name="record">the WARC record</param>
    /// <returns>bytes of the block. null if no block bytes exist for the record.</returns>
    private byte[]? GetBlockBytes(Record record)
    {
        /*
         * Due to the Record class heirarchy, there isn't an easy, consistent way to
         * access the raw bytes that make up the record block. Ideally the
         * records should be refactored / implement interfaces for this, but
         * for now I'll just look at each type to figure out what needs to be
         * output as the block bytes
         */

        switch (record.Type)
        {
            case ContinuationRecord.TypeName:
                return ((ContinuationRecord)record).RecordBlock;

            case ConversionRecord.TypeName:
                return ((ConversionRecord)record).RecordBlock;

            case MetadataRecord.TypeName:
                return ConvertToBytes(((MetadataRecord)record).ContentBlock);

            case RequestRecord.TypeName:
                return ((RequestRecord)record).ContentBlock;

            case ResourceRecord.TypeName:
                return ((ResourceRecord)record).RecordBlock;

            case ResponseRecord.TypeName:
                return ((ResponseRecord)record).ContentBlock;

            case RevisitRecord.TypeName:
                return ConvertToBytes(((RevisitRecord)record).RecordBlock);

            case WarcinfoRecord.TypeName:
                return ConvertToBytes(((WarcinfoRecord)record).ContentBlock);

            default:
                throw new ArgumentException($"Unknown record type '{record.Type}'. Cannot determine block bytes.", nameof(record));
        }
    }

    /// <summary>
    /// helper function to convert UTF8 strings to byte arrays for record blocks.
    /// </summary>
    /// <param name="content">string content to convert.</param>
    /// <returns>byte array of the string content</returns>
    private byte[]? ConvertToBytes(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        return Encoding.UTF8.GetBytes(content);
    }
}