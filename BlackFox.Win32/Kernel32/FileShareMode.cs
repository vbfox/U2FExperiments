using System;

namespace BlackFox.Win32.Kernel32
{
    [Flags]
    public enum FileShareMode : uint
    {
        /// <summary>
        /// Prevents other processes from opening a file or device if they request delete, read, or write access.
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// Enables subsequent open operations on a file or device to request delete access.
        /// </summary>
        Delete = 0x00000004,

        /// <summary>
        /// Enables subsequent open operations on a file or device to request read access.
        /// </summary>
        Read = 0x00000001,

        /// <summary>
        /// Enables subsequent open operations on a file or device to request write access.
        /// </summary>
        Write = 0x00000002,
    }
}
