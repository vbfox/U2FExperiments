// Copyright (c) Julien Roncaglia.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace BlackFox.Binary
{
    internal static class EndianessHelper
    {
        public static Endianness Native { get; } =
            BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

        public static Endianness Resolve(Endianness endianness)
        {
            return endianness == Endianness.Native ? Native : endianness;
        }
    }
}
