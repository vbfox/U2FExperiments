using System;

namespace BlackFox.U2FHid.Core.RawPackets
{
    public class InvalidCommandIdentifierException : InvalidPacketException
    {
        public InvalidCommandIdentifierException(ArraySegment<byte> packet) : base(packet)
        {
        }

        public InvalidCommandIdentifierException(ArraySegment<byte> packet, string message) : base(packet, message)
        {
        }

        public InvalidCommandIdentifierException(ArraySegment<byte> packet, string message, Exception innerException) : base(packet, message, innerException)
        {
        }
    }
}