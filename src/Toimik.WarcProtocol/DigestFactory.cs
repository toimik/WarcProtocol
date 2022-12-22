/*
 * Copyright 2021-2022 nurhafiz@hotmail.sg
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

using System.Security.Cryptography;
using System.Text;

public class DigestFactory
{
    public DigestFactory(string hashName)
    {
        HashName = hashName;
    }

    public string HashName { get; }

    public virtual string CreateDigest(byte[] buffer)
    {
        using var algorithm = HashAlgorithm.Create(HashName);

        // It is assumed that the hash name is valid
        var bytes = algorithm!.ComputeHash(buffer);
        var builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            var value = bytes[i].ToString("X2");
            builder.Append(value);
        }

        return builder.ToString();
    }
}