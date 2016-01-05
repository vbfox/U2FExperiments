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
            OVERLAPPED* lpOverlapped);
    }
}
