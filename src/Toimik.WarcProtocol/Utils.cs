/*
 * Copyright 2021-2023 nurhafiz@hotmail.sg
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

public class Utils
{
    [ExcludeFromCodeCoverage]
    private Utils()
    {
    }

    public static async Task<IDictionary<string, string>> ParseWarcFields(LineReader lineReader)
    {
        var fieldToValue = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var processedFieldToValue = new Dictionary<string, string>();
        string? field = null;
        while (true)
        {
            var line = await lineReader.Read().ConfigureAwait(false);
            var isEofEncountered = line == null;
            if (isEofEncountered)
            {
                var text = "Premature end of file.";
                throw new FormatException(text);
            }

            var isEndOfHeaderEncountered = line == string.Empty;
            if (isEndOfHeaderEncountered)
            {
                break;
            }

            /* Each line can either be a line:
             * - formatted as <name field>:<value>, or
             * - starting with any number of spaces or tabs. In this case, this denotes the
             * continued value for the last read field
             */

            var isValueForPreviousField = line!.StartsWith(' ')
                || line.StartsWith("\\t");
            if (isValueForPreviousField)
            {
                line = RemoveLeadingTabsAndSpaces(line.TrimEnd());
                var isErrorEncountered = field == null;
                if (isErrorEncountered)
                {
                    var text = $"Missing header field for value: {line}";
                    throw new FormatException(text);
                }

                var previousValue = fieldToValue[field!];
                var newValue = $"{previousValue}{line}";
                fieldToValue[field!] = newValue;
            }
            else
            {
                var index = line.IndexOf(':');
                var isErrorEncountered = index == -1;
                if (isErrorEncountered)
                {
                    var text = $"Invalid header field format: {line}";
                    throw new FormatException(text);
                }

                field = line[..index].Trim().ToLower();
                isErrorEncountered = field == string.Empty;
                if (isErrorEncountered)
                {
                    var text = $"Empty header field: {line}";
                    throw new FormatException(text);
                }

                var value = line[(index + 1)..].Trim();
                processedFieldToValue.TryGetValue(field, out string? existingValue);
                isErrorEncountered = existingValue != null
                    && !field.Equals("warc-concurrent-to")
                    && !existingValue.Equals(value);
                if (isErrorEncountered)
                {
                    // Except for WARC-Concurrent-To, having duplicate header fields is
                    // disallowed. If the value is the same, there is no need to add it again.
                    // Otherwise, an exception is thrown.
                    var text = $"Duplicate header: {line}";
                    throw new FormatException(text);
                }

                if (existingValue == null)
                {
                    processedFieldToValue.Add(field, value);
                    fieldToValue.Add(field, value);
                }
            }
        }

        return fieldToValue;
    }

    internal static string? AddBracketsToUri(Uri? uri)
    {
        var text = uri == null
            ? null
            : $"<{uri}>";
        return text;
    }

    internal static Uri CreateId()
    {
        var uri = new Uri($"urn:uuid:{Guid.NewGuid()}");
        return uri;
    }

    internal static string CreateWarcDigest(DigestFactory digestFactory, byte[] block)
    {
        var text = $"{digestFactory.HashName}:{digestFactory.CreateDigest(block)}";
        return text;
    }

    internal static string FormatDate(DateTime date)
    {
        var text = date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        return text;
    }

    internal static int IndexOfPayload(byte[] contentBlock)
    {
        var index = -1;
        const int Offset = 4;
        var length = contentBlock.Length - Offset;
        for (int i = 0; i < length; i++)
        {
            if (contentBlock[i] == WarcParser.CarriageReturn
                && contentBlock[i + 1] == WarcParser.LineFeed
                && contentBlock[i + 2] == WarcParser.CarriageReturn
                && contentBlock[i + 3] == WarcParser.LineFeed)
            {
                index = i;
                break;
            }
        }

        return index;
    }

    internal static Uri RemoveBracketsFromUri(string value)
    {
        // Extract the uri within the angle brackets '<' ... '>'
        var content = value.StartsWith('<') && value.EndsWith('>')
            ? value[1..^1]
            : value;
        return new Uri(content);
    }

    private static string RemoveLeadingTabsAndSpaces(string value)
    {
        var i = 0;
        while (i < value.Length)
        {
            if (value.StartsWith(' '))
            {
                value = value[1..];
                i++;
            }
            else if (value.StartsWith("\\t"))
            {
                value = value[2..];
                i += 2;
            }
            else
            {
                break;
            }
        }

        return value;
    }
}