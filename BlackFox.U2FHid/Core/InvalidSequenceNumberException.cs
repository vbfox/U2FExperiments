using System;
using BlackFox.U2FHid.Core.RawPackets;

namespace BlackFox.U2FHid.Core
{
    internal class InvalidSequenceNumberException : InvalidPacketException
    {
        public InvalidSequenceNumberException(ArraySegment<byte> packet) : base(packet)
        {
        }

        public InvalidSequenceNumberException(ArraySegment<byte> packet, string message) : base(packet, message)
        {
        }

        public InvalidSequenceNumberException(ArraySegment<byte> packet, string message, Exception innerException) : base(packet, message, innerException)
        {
        }
    }
}