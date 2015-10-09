namespace BlackFox.U2FHid
{
    enum U2FHidErrors : byte
    {
        /// <summary>
        /// No error
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Invalid command
        /// </summary>
        InvalidCommand = 0x01,

        /// <summary>
        /// Invalid parameter
        /// </summary>
        InvalidParameter = 0x02,

        /// <summary>
        /// Invalid message length
        /// </summary>
        InvalidLength = 0x03,

        /// <summary>
        /// Invalid message sequencing
        /// </summary>
        InvalidSequencing = 0x04,

        /// <summary>
        /// Message has timed out
        /// </summary>
        Timeout = 0x05,

        /// <summary>
        /// Channel busy
        /// </summary>
        Busy = 0x06,

        /// <summary>
        /// Command requires channel lock
        /// </summary>
        LockRequired = 0x0A,

        /// <summary>
        /// SYNC command failed
        /// </summary>
        SyncFailed = 0x0B,

        /// <summary>
        /// Unspecified error
        /// </summary>
        Unspecified = 0x7F,
    }
}