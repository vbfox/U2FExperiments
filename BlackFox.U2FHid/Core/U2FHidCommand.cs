namespace BlackFox.U2FHid.Core
{
    public enum U2FHidCommand : byte
    {
        /// <summary>
        /// Echo data through local processor only
        /// </summary>
        Ping = 0x80 | 0x01,

        /// <summary>
        /// Perform reset action and read ATR string
        /// </summary>
        ResetAndAtr = 0x80 | 0x02,

        /// <summary>
        /// Send U2F message frame
        /// </summary>
        Message = 0x80 | 0x03,

        /// <summary>
        /// Send lock channel command
        /// </summary>
        Lock = 0x80 | 0x04,

        /// <summary>
        /// Obtain system information record
        /// </summary>
        SysInfo = 0x80 | 0x05,

        /// <summary>
        /// Channel initialization
        /// </summary>
        Init = 0x80 | 0x06,

        /// <summary>
        /// Control prompt flashing
        /// </summary>
        Prompt = 0x80 | 0x07,

        /// <summary>
        /// Send device identification wink
        /// </summary>
        Wink = 0x80 | 0x08,

        /// <summary>
        /// USB test
        /// </summary>
        UsbTest = 0x80 | 0x39,

        /// <summary>
        /// Device Firmware Upgrade
        /// </summary>
        Dfu = 0x80 | 0x3a,

        /// <summary>
        /// Protocol resync command
        /// </summary>
        Sync = 0x80 | 0x3c,

        /// <summary>
        /// Error response
        /// </summary>
        Error = 0x80 | 0x3f,

        /// <summary>
        /// First vendor defined command
        /// </summary>
        VendorFirst = 0x80 | 0x40,

        /// <summary>
        /// Last vendor defined command
        /// </summary>
        VendorLast = 0x80 | 0x7f
    }
}