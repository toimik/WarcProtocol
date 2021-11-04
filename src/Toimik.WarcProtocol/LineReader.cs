/*
 * Copyright 2021 nurhafiz@hotmail.sg
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

namespace Toimik.WarcProtocol
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class LineReader
    {
        private static readonly IList<int> EolCharacters = new List<int>
        {
            WarcParser.CarriageReturn,
            WarcParser.LineFeed,
        };

        public LineReader(Stream stream, CancellationToken cancellationToken)
        {
            Stream = stream;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public Stream Stream { get; }

        public async Task Offset(long byteOffset)
        {
            // NOTE: This is naively done because seek is unsupported by the underlying class
            for (long i = 0; i < byteOffset; i++)
            {
                var readCount = await Stream.ReadAsync(buffer: (new byte[1]).AsMemory(start: 0, length: 1));
                var isEofEncountered = readCount == 0;
                if (isEofEncountered)
                {
                    throw new ArgumentException("Offset exceeds file size.", nameof(byteOffset));
                }
            }
        }

        public async Task<string> Read()
        {
            // NOTE: A line is terminated by consecutive occurrences of the EOL characters
            var readBytes = new List<byte>();
            do
            {
                CancellationToken.ThrowIfCancellationRequested();
                var buffer = new byte[1];
                var readCount = await Stream.ReadAsync(buffer.AsMemory(start: 0, length: 1));
                var isEofEncountered = readCount == 0;
                if (isEofEncountered)
                {
                    // Treat EOF as per normal only if it is empty. Otherwise, it is assumed that
                    // the EOL characters are found.
                    if (readBytes.Count == 0)
                    {
                        readBytes = null;
                    }

                    break;
                }

                var currentByte = buffer[0];
                readBytes.Add(currentByte);

                var readByteCount = readBytes.Count;
                var eolCharacterCount = EolCharacters.Count;
                if (readByteCount >= eolCharacterCount)
                {
                    // Check if the list of read bytes ends with the sequence of EOL characters
                    var isSequenceFound = true;
                    var offset = readByteCount - eolCharacterCount;
                    for (int i = 0, j = offset; i < eolCharacterCount; i++, j++)
                    {
                        var eolCharacter = EolCharacters[i];
                        var readByte = readBytes[j];
                        if (eolCharacter != readByte)
                        {
                            isSequenceFound = false;
                            break;
                        }
                    }

                    if (isSequenceFound)
                    {
                        // Remove the sequence
                        readBytes.RemoveRange(offset, eolCharacterCount);
                        break;
                    }
                }
            }
            while (true);

            var line = readBytes == null
                ? null
                : Encoding.UTF8.GetString(readBytes.ToArray());
            return line;
        }
    }
}