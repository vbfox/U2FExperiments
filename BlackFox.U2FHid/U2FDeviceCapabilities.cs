using System;

namespace BlackFox.U2FHid
{
    [Flags]
    public enum U2FDeviceCapabilities : byte
    {
        /// <summary>
        /// Device supports WINK command
        /// </summary>
        Wink = 0x01
    }
}