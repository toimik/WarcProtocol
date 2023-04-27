﻿/*
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

public class PayloadTypeIdentifier
{
    internal static readonly int[] DefaultDelimiter = new int[]
    {
        WarcParser.CarriageReturn,
        WarcParser.LineFeed,
        WarcParser.CarriageReturn,
        WarcParser.LineFeed,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PayloadTypeIdentifier"/> class.
    /// </summary>
    /// <param name="delimiter">A sequence of characters that identifies the start of a payload. If this is <c>null</c>, it defaults to two pairs of <c>CRLF</c>, which is the one commonly used by HTTP.</c></param>
    public PayloadTypeIdentifier(int[]? delimiter = null)
    {
        Delimiter = delimiter ?? DefaultDelimiter;
    }

    public int[] Delimiter { get; }

    public virtual string? Identify(byte[] payload)
    {
        // NOTE: This method is not implemented
        return null;
    }

    /// <summary>
    /// Gets the index of the payload, if any.
    /// </summary>
    /// <param name="contentBlock">The bytes representing the content block of a <see cref="Record"/>.</param>
    /// <returns>The start index of the payload or <c>-1</c> if none is found.</returns>
    /// <remarks>The <paramref name="contentBlock"/> is searched to find the first occurrence of the <see cref="Delimiter"/>.</remarks>
    public int IndexOfPayload(byte[] contentBlock) => Utils.IndexOfPayload(contentBlock, Delimiter);
}