using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.Win32.Kernel32
{
    [SuppressUnmanagedCodeSecurity]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Kernell32DllNativeMethods
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
        public static extern unsafe bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize,
            out int pBytesReturned,
            NativeOverlapped* lpOverlapped);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool GetOverlappedResult(
            SafeFileHandle hDevice,
            NativeOverlapped* lpOverlapped,
            out int lpNumberOfBytesTransferred,
            bool wait);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool WriteFile(
            SafeFileHandle hFile,
            IntPtr lpBuffer,
            int nNumberOfBytesToWrite,
            out int lpNumberOfBytesWritten,
            NativeOverlapped* lpOverlapped);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool ReadFile(
            SafeFileHandle hFile,
            IntPtr lpBuffer,
            int nNumberOfBytesToRead,
            out int lpNumberOfBytesRead,
            NativeOverlapped* lpOverlapped);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CancelIo(
            SafeFileHandle hFile);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool CancelIoEx(
            SafeFileHandle hFile,
            NativeOverlapped* lpOverlapped);
    }
}
