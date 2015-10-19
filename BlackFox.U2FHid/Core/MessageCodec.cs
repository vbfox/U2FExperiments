using System;
using BlackFox.Binary;
using static BlackFox.U2FHid.Core.U2FHidConsts;

namespace BlackFox.U2FHid.Core
{
    public static class MessageCodec
    {
        public static U2FDeviceInfo DecodeInitResponse(ArraySegment<byte> raw, out ArraySegment<byte> nonce)
        {
            if (raw.Count < 17)
            {
                throw new ArgumentException("The data is too small for an Init response");
            }

            nonce = raw.Segment(0, InitNonceSize);

            var afterNonce = raw.Segment(InitNonceSize);
            using (var reader = new EndianReader(afterNonce.AsStream(), Endianness.BigEndian))
            {
                var channel = reader.ReadUInt32();
                var protocolVersion = reader.ReadByte();
                var majorVersionNumber = reader.ReadByte();
                var minorVersionNumber = reader.ReadByte();
                var buildVersionNumber = reader.ReadByte();
                var capabilities = (U2FDeviceCapabilities)reader.ReadByte();

                var version = new Version(majorVersionNumber, minorVersionNumber, buildVersionNumber);
                var parsedResponse = new U2FDeviceInfo(channel, protocolVersion, version, capabilities);

                return parsedResponse;
            }
        }
    }
}
