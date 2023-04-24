![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/nurhafiz/315596422731782085c250a859a3cc38/raw/WarcProtocol-coverage.json)
![Nuget](https://img.shields.io/nuget/v/Toimik.WarcProtocol)

# Toimik.WarcProtocol

.NET 6 C# [WARC](https://iipc.github.io/warc-specifications/specifications/warc-format/warc-1.1) parser and writer.

## Features

- Parses uncompressed / compressed - as a whole or per record - Web ARChive (WARC) version 1.0 / 1.1 files (via `WarcParser` class)
- Option to terminate or resume processing upon encountering malformed content
- Creates WARC records with auto-generated `WARC-Block-Digest` / `WARC-Payload-Digest` using configurable hash algorithm
- Writes uncompressed or per-record compressed WARC version 1.0 / 1.1 files (via `WarcWriter` class)

## Quick Start

### Installation

#### Package Manager

```command
PM> Install-Package Toimik.WarcProtocol
```

#### .NET CLI

```command
> dotnet add package Toimik.WarcProtocol
```

### WarcParser Usage

The `WarcParser` class can be used to parse WARC 1.0 and 1.1 files.

```c# 
using System.Diagnostics;
using System.Threading.Tasks;
using Toimik.WarcProtocol;

class Program
{
    static async Task Main(string[] args)
    {
        var parser = new WarcParser();

        // Path to a WARC file that contains one or more records, which may be uncompressed or
        // compressed (as a whole or per record)
        var path = "uncompressed.warc";

        // Use a '.gz' file extension to indicate that the file is compressed using GZip
        // var path "compressed.warc.gz";

        // In case any part of the WARC file is malformed and parsing is expected to resume -
        // rather than throwing an exception and terminates - a parse log is specified
        var parseLog = new DebugParseLog();

        // Parse the file and process the records accordingly
        var records = parser.Parse(path, parseLog);

        // Alternatively, use a stream:
        // var stream = ...
        // var isCompressed = ... // true if the stream is compressed; false otherwise
        // var records = parser.Parse(stream, isCompressed, parseLog);

        await foreach (WarcProtocol.Record record in records)
        {
            switch (record.Type)
            {
                case ContinuationRecord.TypeName:

                    // ...
                    break;

                case ConversionRecord.TypeName:

                    // ...
                    break;

                case MetadataRecord.TypeName:

                    // ...
                    break;

                case RequestRecord.TypeName:

                    // ...
                    break;

                case ResourceRecord.TypeName:

                    // ...
                    break;

                case ResponseRecord.TypeName:

                    // ...
                    break;

                case RevisitRecord.TypeName:

                    // ...
                    break;

                case WarcinfoRecord.TypeName:

                    // ...
                    break;

                case "custom-type":

                    // ...
                    break;
            }
        }
    }

    class DebugParseLog : IParseLog
    {
        public void ChunkSkipped(string chunk)
        {
            Debug.WriteLine(chunk);
        }

        public void ErrorEncountered(string error)
        {
            Debug.WriteLine(error);
        }
    }
}
```

### WarcWriter Usage

The `WarcWriter` class can be used to write WARC records to a file.

```c# 
using Toimik.WarcProtocol;

class Program
{
    static void Main(string[] args)
    {
        // Creates a new WARC file with per-record compression
        // Wrap the writer in a "using" block to ensure data
        // is properly flushed to the file.
        // Or call "Close()" directly
		using(var warcWriter = new WarcWriter("example.warc.gz"))
		{
			warchWriter.WriteRecord(warcInfoRecord);
			warchWriter.WriteRecord(requestRecord);
			warchWriter.WriteRecord(responseRecord);
		}
        
        // You can also create uncompressed WARCs. This is controlled via the file extension.
        using(var writer = new WarcWriter("uncompressed.warc"))
        {
			// ...
        }
        
        // You can also force per-record compression, regardless of file extension
        using(var writer = new WarcWriter("actually-compressed.warc.whatever", true))
        {
			// ...
        }
    }
}
```