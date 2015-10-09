using System;

namespace BlackFox.U2FHid.Core.RawPackets
{
    public struct U2FInitializationPacket
    {
        public uint ChannelIdentifier;
        public byte CommandIdentifier;
        public ushort PayloadLength;
        public ArraySegment<byte> Data;

        public static readonly int NoDataSize = 4 + 1 + 2;
    }
}