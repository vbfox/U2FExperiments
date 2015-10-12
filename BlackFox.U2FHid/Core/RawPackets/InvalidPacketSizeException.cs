using System;

namespace BlackFox.U2FHid.Core.RawPackets
{
    public class InvalidPacketSizeException : InvalidPacketException
    {
        public InvalidPacketSizeException(ArraySegment<byte> packet) : base(packet)
        {
        }

        public InvalidPacketSizeException(ArraySegment<byte> packet, string message) : base(packet, message)
        {
        }

        public InvalidPacketSizeException(ArraySegment<byte> packet, string message, Exception innerException) : base(packet, message, innerException)
        {
        }
    }
}