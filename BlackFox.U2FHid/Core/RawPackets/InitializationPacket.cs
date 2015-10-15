using System;
using System.Diagnostics;
using System.IO;
using BlackFox.Binary;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Core.RawPackets
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct InitializationPacket
    {
        public uint ChannelIdentifier;
        public byte CommandIdentifier;
        public ushort PayloadLength;
        public ArraySegment<byte> Data;

        public static readonly int NoDataSize = 4 + 1 + 2;

        private string DebuggerDisplay =>
            $"Initialization Paket 0x{CommandIdentifier:X2}, {Data.Count} bytes on channel 0x{ChannelIdentifier:X8}";

        public static InitializationPacket ReadFrom(ArraySegment<byte> segment)
        {
            if (segment.Count < NoDataSize)
            {
                throw new InvalidPacketSizeException(segment,
                    $"Data is too small, expected {NoDataSize} bytes but provided only {segment.Count}");
            }

            var result = new InitializationPacket();

            using (var stream = new MemoryStream(segment.Array, segment.Offset, segment.Count))
            {
                var reader = new BinaryReader(stream);
                result.ChannelIdentifier = reader.ReadUInt32();
                result.CommandIdentifier = reader.ReadByte();

                CheckCommand(segment, result.CommandIdentifier);

                var payloadLengthHi = reader.ReadByte();
                var payloadLengthLo = reader.ReadByte();
                result.PayloadLength = (ushort)(payloadLengthHi << 8 | payloadLengthLo);
            }

            var remainingBytes = Math.Min(segment.Count - NoDataSize, result.PayloadLength);
            result.Data = new ArraySegment<byte>(segment.Array, segment.Offset + NoDataSize,
                remainingBytes);

            return result;
        }

        public static bool IsCommand(byte identifier)
        {
            return (identifier & 0x80) != 0x00;
        }

        private static void CheckCommand(ArraySegment<byte> data, byte commandIdentifier)
        {
            if (!IsCommand(commandIdentifier))
            {
                throw new InvalidCommandIdentifierException(data,
                    $"The command ID is invalid (0x{commandIdentifier:X2}) it might be a continuation one");
            }
        }

        public void WriteTo([NotNull] Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var writer = new BinaryWriter(stream);
            writer.Write(ChannelIdentifier);
            writer.Write(CommandIdentifier);
            writer.Write((byte)((PayloadLength >> 8) & 0xFF));
            writer.Write((byte)((PayloadLength >> 0) & 0xFF));
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