using System;
using System.Diagnostics;
using System.IO;
using BlackFox.BinaryUtils;
using BlackFox.UsbHid;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Core.RawPackets
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct ContinuationPacket
    {
        public uint ChannelIdentifier;
        public byte PaketSequence;
        public ArraySegment<byte> Data;

        public static readonly int NoDataSize = 4 + 1;

        private string DebuggerDisplay =>
            $"Continuation Paket {PaketSequence} of sequence, {Data.Count} bytes on channel 0x{ChannelIdentifier:X8}";

        public static ContinuationPacket ReadFrom(ArraySegment<byte> segment, int maxDataSize)
        {
            if (segment.Count < NoDataSize)
            {
                throw new InvalidPacketSizeException(segment,
                    $"Data is too small, expected {NoDataSize} bytes but provided only {segment.Count}");
            }

            var result = new ContinuationPacket();

            using (var stream = new MemoryStream(segment.Array, segment.Offset, segment.Count))
            {
                var reader = new BinaryReader(stream);
                result.ChannelIdentifier = reader.ReadUInt32();
                result.PaketSequence = reader.ReadByte();

                CheckSequence(segment, result.PaketSequence);
            }

            var remainingBytes = Math.Min(maxDataSize, segment.Count - NoDataSize);
            result.Data = new ArraySegment<byte>(segment.Array, segment.Offset + NoDataSize, remainingBytes);

            return result;
        }

        public static bool IsSequence(byte identifier)
        {
            return (identifier & 0x80) == 0x00;
        }

        private static void CheckSequence(ArraySegment<byte> data, byte commandIdentifier)
        {
            if (!IsSequence(commandIdentifier))
            {
                throw new InvalidSequenceNumberException(data,
                    $"The sequence number is invalid (0x{commandIdentifier:X2}) it might be an initialization one");
            }
        }

        public void WriteTo([NotNull] Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var writer = new BinaryWriter(stream);
            writer.Write(ChannelIdentifier);
            writer.Write(PaketSequence);
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