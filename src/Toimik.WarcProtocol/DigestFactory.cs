/*
 * Copyright 2021-2024 nurhafiz@hotmail.sg
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

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

public class DigestFactory(string hashName)
{
    public string HashName { get; } = hashName;

    [ExcludeFromCodeCoverage]
    public virtual string CreateDigest(byte[] buffer)
    {
        IDigest digest = HashName.ToUpper() switch
        {
            "BLAKE2B" => new Blake2bDigest(),
            "BLAKE2S" => new Blake2sDigest(),
            "BLAKE2XS" => new Blake2xsDigest(),
            "GOST3411_2012_256" => new Gost3411_2012_256Digest(),
            "GOST3411_2012_512" => new Gost3411_2012_512Digest(),
            "GOST3411" => new Gost3411Digest(),
            "HARAKA256" => new Haraka256Digest(),
            "HARAKA512" => new Haraka512Digest(),
            "KECCAK" => new KeccakDigest(),
            "MD2" => new MD2Digest(),
            "MD4" => new MD4Digest(),
            "MD5" => new MD5Digest(),
            "NULL" => new NullDigest(),
            "RIPEMD128" => new RipeMD128Digest(),
            "RIPEMD160" => new RipeMD160Digest(),
            "RIPEMD256" => new RipeMD256Digest(),
            "RIPEMD320" => new RipeMD320Digest(),
            "SHA1" => new Sha1Digest(),
            "SHA224" => new Sha224Digest(),
            "SHA256" => new Sha256Digest(),
            "SHA384" => new Sha384Digest(),
            "SHA3" => new Sha3Digest(),
            "SHA512" => new Sha512Digest(),
            "SHAKE" => new ShakeDigest(),
            "SM3" => new SM3Digest(),
            "TIGER" => new TigerDigest(),
            "WHIRLPOOL" => new WhirlpoolDigest(),
            _ => throw new ArgumentException($"Unsupported hash algorithm: {HashName}"),
        };
        digest.Reset();
        digest.BlockUpdate(
            buffer,
            0,
            buffer.Length);

        byte[] bytes = new byte[digest.GetDigestSize()];
        digest.DoFinal(bytes, 0);

        var builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            var value = bytes[i].ToString("X2");
            builder.Append(value);
        }

        return builder.ToString();
    }
}