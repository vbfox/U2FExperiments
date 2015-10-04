using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace U2FExperiments.Win32.Hid
{
    static class HidDll
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport("hid.dll", SetLastError = true, EntryPoint = "HidD_GetHidGuid")]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static extern void GetHidGuid(out Guid gHid);

        public static Guid HidGuid
        {
            get
            {
                Guid guid;
                GetHidGuid(out guid);
                return guid;
            }
        }

        /* gets hid device attributes */

        [SuppressUnmanagedCodeSecurity]
        [DllImport("hid.dll", SetLastError = true, EntryPoint = "HidD_GetAttributes")]
        public static extern bool GetAttributes(IntPtr hFile,
            ref HiddAttributes attributes);

        /* gets usb manufacturer string */

        [SuppressUnmanagedCodeSecurity]
        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "HidD_GetManufacturerString")]
        public static extern bool GetManufacturerString(IntPtr hFile,
            StringBuilder buffer, int bufferLength);

        /* gets product string */

        [SuppressUnmanagedCodeSecurity]
        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "HidD_GetProductString")]
        public static extern bool GetProductString(IntPtr hFile,
            StringBuilder buffer, int bufferLength);

        /* gets serial number string */

        [SuppressUnmanagedCodeSecurity]
        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "HidD_GetSerialNumberString")]
        internal static extern bool GetSerialNumberString(IntPtr hDevice,
            StringBuilder buffer, int bufferLength);
    }
}
