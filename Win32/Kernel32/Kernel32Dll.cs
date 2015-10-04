using System;
using System.Runtime.InteropServices;

namespace U2FExperiments.Win32.Kernel32
{
    static class Kernel32Dll
    {
        /// <summary>
        /// <para>Creates or opens a file or I/O device.</para>
        /// <para>The most commonly used I/O devices are as follows: file, file stream, directory, physical disk,
        /// volume, console buffer, tape drive, communications resource, mailslot, and pipe. The function returns a
        /// handle that can be used to access the file or device for various types of I/O depending on the file or
        /// device and the flags and attributes specified.</para>
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateFile(
            [MarshalAs(UnmanagedType.LPStr)] string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}