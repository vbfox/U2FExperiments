using BlackFox.U2FHid.Utils;

namespace BlackFox.U2FHid
{
    enum U2FHidErrors : byte
    {
        /// <summary>
        /// No error
        /// </summary>
        [EnumDescription("No error")]
        None = 0x00,

        /// <summary>
        /// Invalid command
        /// </summary>
        [EnumDescription("Invalid command")]
        InvalidCommand = 0x01,

        /// <summary>
        /// Invalid parameter
        /// </summary>
        [EnumDescription("Invalid parameter")]
        InvalidParameter = 0x02,

        /// <summary>
        /// Invalid message length
        /// </summary>
        [EnumDescription("Invalid message length")]
        InvalidLength = 0x03,

        /// <summary>
        /// Invalid message sequencing
        /// </summary>
        [EnumDescription("Invalid message sequencing")]
        InvalidSequencing = 0x04,

        /// <summary>
        /// Message has timed out
        /// </summary>
        [EnumDescription("Message has timed out")]
        Timeout = 0x05,

        /// <summary>
        /// Channel is busy
        /// </summary>
        [EnumDescription("Channel is busy")]
        Busy = 0x06,

        /// <summary>
        /// Command requires channel lock
        /// </summary>
        [EnumDescription("Command requires channel lock")]
        LockRequired = 0x0A,

        /// <summary>
        /// SYNC command failed, initialization required
        /// </summary>
        [EnumDescription("SYNC command failed, initialization required")]
        SyncFailed = 0x0B,

        /// <summary>
        /// Unspecified error
        /// </summary>
        [EnumDescription("Unspecified error")]
        Unspecified = 0x7F
    }
}