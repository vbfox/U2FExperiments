using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using static PInvoke.Kernel32;

namespace BlackFox.Win32.Kernel32
{
    [SuppressUnmanagedCodeSecurity]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public static class Kernell32DllNativeMethods
    {
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool DeviceIoControl(
            SafeObjectHandle hDevice,
            uint dwIoControlCode,
            IntPtr inBuffer,
            int nInBufferSize,
            IntPtr outBuffer,
            int nOutBufferSize,
            out int pBytesReturned,
            NativeOverlapped* lpOverlapped);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool GetOverlappedResult(
            SafeObjectHandle hDevice,
            NativeOverlapped* lpOverlapped,
            out int lpNumberOfBytesTransferred,
            bool wait);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool WriteFile(
            SafeObjectHandle hFile,
            IntPtr lpBuffer,
            int nNumberOfBytesToWrite,
            out int lpNumberOfBytesWritten,
            NativeOverlapped* lpOverlapped);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool ReadFile(
            SafeObjectHandle hFile,
            IntPtr lpBuffer,
            int nNumberOfBytesToRead,
            out int lpNumberOfBytesRead,
            NativeOverlapped* lpOverlapped);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CancelIo(
            SafeObjectHandle hFile);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern unsafe bool CancelIoEx(
            SafeObjectHandle hFile,
            NativeOverlapped* lpOverlapped);
    }
}
