using System;

namespace BlackFox.UsbHid.Portable
{
    [Flags]
    public enum HidDeviceAccessMode
    {
        /// <summary>
        /// Read from the device.
        /// </summary>
        Read = 1,
        /// <summary>
        /// Write to the device.
        /// </summary>
        Write = 2,

        /// <summary>
        /// Read and write from/to the device.
        /// </summary>
        ReadWrite = Read | Write
    }
}
