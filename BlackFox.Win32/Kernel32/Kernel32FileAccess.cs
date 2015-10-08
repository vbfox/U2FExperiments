using System;

namespace BlackFox.Win32.Kernel32
{
    [Flags]
    public enum Kernel32FileAccess : uint
    {
        None = 0x00000000,
        Delete = 0x00010000,
        ReadControl = 0x00020000,
        WriteDac = 0x00040000,
        WriteOwner = 0x00080000,
        Synchronize = 0x00100000,

        StandardRightsRequired = 0x000F0000,

        StandardRightsRead = ReadControl,
        StandardRightsWrite = ReadControl,
        StandardRightsExecute = ReadControl,

        StandardRightsAll = 0x001F0000,

        SpecificRightsAll = 0x0000FFFF,

        AccessSystemSecurity = 0x01000000,

        MaximumAllowed = 0x02000000,

        GenericRead = 0x80000000,
        GenericWrite = 0x40000000,
        GenericExecute = 0x20000000,
        GenericAll = 0x10000000
    }
}