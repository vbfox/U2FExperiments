using System;
using BlackFox.UsbHid;

namespace BlackFox.U2FHid.Core
{
    public struct FidoU2FHidMessage
    {
        public uint Channel { get; }
        public U2FHidCommand Command { get; }
        public ArraySegment<byte> Data { get; }

        public FidoU2FHidMessage(uint channel, U2FHidCommand command, ArraySegment<byte> data)
        {
            Channel = channel;
            Command = command;
            Data = data;
        }

        public FidoU2FHidMessage(uint channel, U2FHidCommand command)
            : this(channel, command, EmptyArraySegment<byte>.Value)
        {
        }
    }
}