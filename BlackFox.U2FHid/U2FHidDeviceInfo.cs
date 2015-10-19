using System;

namespace BlackFox.U2FHid
{
    public struct U2FHidDeviceInfo
    {
        public uint Channel { get; }
        public byte ProtocolVersion { get; }
        public Version Version { get; }
        public U2FDeviceCapabilities Capabilities { get; }

        public U2FHidDeviceInfo(uint channel, byte protocolVersion, Version version, U2FDeviceCapabilities capabilities)
        {
            Channel = channel;
            ProtocolVersion = protocolVersion;
            Version = version;
            Capabilities = capabilities;
        }
    }
}