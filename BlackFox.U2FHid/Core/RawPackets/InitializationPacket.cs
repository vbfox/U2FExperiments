using System;
using System.IO;
using BlackFox.UsbHid.Portable;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Core.RawPackets
{
    public struct InitializationPacket
    {
        public uint ChannelIdentifier;
        public byte CommandIdentifier;
        public ushort PayloadLength;
        public ArraySegment<byte> Data;

        public static readonly int NoDataSize = 4 + 1 + 2;

        public static InitializationPacket ReadFrom(ArraySegment<byte> segment)
        {
            if (segment.Count < NoDataSize)
            {
                throw new ArgumentException(
                    $"Data is too small, expected {NoDataSize} bytes but provided only {segment.Count}",
                    nameof(segment));
            }

            var result = new InitializationPacket();

            using (var stream = new MemoryStream(segment.Array, segment.Offset, segment.Count))
            {
                var reader = new BinaryReader(stream);
                result.ChannelIdentifier = reader.ReadUInt32();
                result.CommandIdentifier = reader.ReadByte();

                var payloadLengthHi = reader.ReadByte();
                var payloadLengthLo = reader.ReadByte();
                result.PayloadLength = (ushort)(payloadLengthHi << 8 | payloadLengthLo);
            }

            var remainingBytes = Math.Min(segment.Count - NoDataSize, result.PayloadLength);
            result.Data = new ArraySegment<byte>(segment.Array, segment.Offset + NoDataSize,
                remainingBytes);

            return result;
        }

        public void WriteTo([NotNull] Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var writer = new BinaryWriter(stream);
            writer.Write(ChannelIdentifier);
            writer.Write(CommandIdentifier);
            writer.Write((PayloadLength >> 8) & 0xFF);
            writer.Write((PayloadLength >> 0) & 0xFF);
            stream.Write(Data.Array, Data.Offset, Data.Count);
        }

        public void WriteTo(ArraySegment<byte> segment)
        {
            using (var stream = segment.AsStream())
            {
                WriteTo(stream);
            }
        }
    }
}