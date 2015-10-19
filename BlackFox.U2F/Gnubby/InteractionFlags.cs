using System;

namespace BlackFox.U2F.Gnubby
{
    [Flags]
    public enum InteractionFlags : byte
    {
        TestOfUserPresenceRequired = 0x01,
        ConsumeTestOfUserPresence = 0x02,
        TestSignatureOnly = 0x04,
        AttestWithDeviceKey = 0x80
    }
}