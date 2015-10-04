using System;
using System.Runtime.InteropServices;
using System.Text;

namespace U2FExperiments.Win32.Hid
{
    static class HidDll
    {
        [DllImport("hid.dll", SetLastError = true)]
        /* gets HID class Guid */
        public static extern void GetHidGuid(out Guid gHid);

        /* gets hid device attributes */
        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool GetAttributes(IntPtr hFile,
            ref HiddAttributes attributes);

        /* gets usb manufacturer string */
        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetManufacturerString(IntPtr hFile,
            StringBuilder buffer, int bufferLength);

        /* gets product string */
        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetProductString(IntPtr hFile,
            StringBuilder buffer, int bufferLength);

        /* gets serial number string */
        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GetSerialNumberString(IntPtr hDevice,
            StringBuilder buffer, int bufferLength);
    }
}