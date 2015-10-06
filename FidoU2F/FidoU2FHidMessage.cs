using System;

namespace U2FExperiments.FidoU2F
{
    public class FidoU2FHidMessage
    {
        public uint ChannelIdentifier { get; }
        public U2FHidCommand CommandIdentifier { get; }
        public ArraySegment<byte> Data { get; }

        public FidoU2FHidMessage(uint channelIdentifier, U2FHidCommand commandIdentifier, ArraySegment<byte> data)
        {
            ChannelIdentifier = channelIdentifier;
            CommandIdentifier = commandIdentifier;
            Data = data;
        }
    }
}