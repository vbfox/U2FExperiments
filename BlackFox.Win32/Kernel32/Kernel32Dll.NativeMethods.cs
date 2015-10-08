using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.Win32.Kernel32
{
    partial class Kernel32Dll
    {
        [SuppressUnmanagedCodeSecurity]
        [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
        public static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "CreateFile")]
            public static extern SafeFileHandle CreateFile(
                [MarshalAs(UnmanagedType.LPStr)] string lpFileName,
                Kernel32FileAccess dwDesiredAccess,
                FileShareMode dwShareMode,
                IntPtr lpSecurityAttributes,
                FileCreationDisposition dwCreationDisposition,
                FileFlags dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                uint dwIoControlCode,
                IntPtr inBuffer,
                int nInBufferSize,
                IntPtr outBuffer,
                int nOutBufferSize,
                out int pBytesReturned,
                IntPtr lpOverlapped);

            [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool GetOverlappedResult(
                SafeFileHandle hDevice,
                IntPtr lpOverlapped,
                out int lpNumberOfBytesTransferred,
                bool wait);

            [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool WriteFile(
                SafeFileHandle hFile,
                IntPtr lpBuffer,
                int nNumberOfBytesToWrite,
                out int lpNumberOfBytesWritten,
                IntPtr lpOverlapped);

            [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool ReadFile(
                SafeFileHandle hFile,
                IntPtr lpBuffer,
                int nNumberOfBytesToRead,
                out int lpNumberOfBytesRead,
                IntPtr lpOverlapped);
        }
    }
}
