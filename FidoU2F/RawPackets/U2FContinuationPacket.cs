using System;

namespace U2FExperiments.FidoU2F
{
    public struct U2FContinuationPacket
    {
        public uint ChannelIdentifier;
        public byte PaketSequence;
        public ArraySegment<byte> Data;

        public static readonly int NoDataSize = 4 + 1;
    }
}