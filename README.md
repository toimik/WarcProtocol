![GitHub Workflow Status](https://img.shields.io/github/workflow/status/toimik/WarcProtocol/CI)
![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/nurhafiz/315596422731782085c250a859a3cc38/raw/WarcProtocol-coverage.json)
![Nuget](https://img.shields.io/nuget/v/Toimik.WarcProtocol)

# Toimik.WarcProtocol

.NET 5 C# [WARC](https://iipc.github.io/warc-specifications/specifications/warc-format/warc-1.1) parser.

## Features

- Parses uncompressed / compressed - as a whole or per record - Web ARChive (WARC) version 1.0 / 1.1 files
- Option to terminate or resume processing upon encountering malformed content
- Creates WARC records with auto-generated `WARC-Block-Digest` / `WARC-Payload-Digest` using configurable hash algorithm

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

### Usage

```c# 
using System.Diagnostics;

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
            switch (record.Type.ToLower())
            {
                case "continuation":

                    // ...
                    break;

                case "conversion":

                    // ...
                    break;

                case "metadata":

                    // ...
                    break;

                case "request":

                    // ...
                    break;

                case "resource":

                    // ...
                    break;

                case "response":

                    // ...
                    break;

                case "revisit":

                    // ...
                    break;

                case "warcinfo":

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