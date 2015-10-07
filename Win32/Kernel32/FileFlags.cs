using System;

namespace U2FExperiments.Win32.Kernel32
{
    [Flags]
    internal enum FileFlags : uint
    {
        None = 0x00000000,
        BackupSemantics = 0x02000000,
        DeleteOnClose = 0x04000000,
        NoBuffering = 0x20000000,
        OpenNoRecall = 0x00100000,
        OpenReparsePoint = 0x00200000,
        Overlapped = 0x40000000,
        PosixSemantics = 0x01000000,
        RandomAccess = 0x10000000,
        SequentialScan = 0x08000000,
        WriteThrough = 0x80000000
    }
}