using System;
using System.IO;
using BlackFox.UsbHid.Portable;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Core.RawPackets
{
    public struct ContinuationPacket
    {
        public uint ChannelIdentifier;
        public byte PaketSequence;
        public ArraySegment<byte> Data;

        public static readonly int NoDataSize = 4 + 1;

        public static ContinuationPacket ReadFrom(ArraySegment<byte> segment)
        {
            throw new NotImplementedException();
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