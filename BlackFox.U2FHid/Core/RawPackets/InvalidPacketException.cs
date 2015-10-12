using System;

namespace BlackFox.U2FHid.Core.RawPackets
{
    public class InvalidPacketException : Exception
    {
        public ArraySegment<byte> Packet { get; }

        public InvalidPacketException(ArraySegment<byte> packet)
        {
            Packet = packet;
        }

        public InvalidPacketException(ArraySegment<byte> packet, string message) : base(message)
        {
            Packet = packet;
        }

        public InvalidPacketException(ArraySegment<byte> packet, string message, Exception innerException) : base(message, innerException)
        {
            Packet = packet;
        }
    }
}