using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.Win32.Hid
{
    [SuppressUnmanagedCodeSecurity]
    public static class HidDllNativeMethods
    {
        [DllImport("hid.dll", SetLastError = true, EntryPoint = "HidD_GetHidGuid")]
        public static extern void HidD_GetHidGuid(out Guid gHid);

        [DllImport("hid.dll", SetLastError = true, EntryPoint = "HidD_GetAttributes")]
        public static extern bool HidD_GetAttributes(SafeFileHandle hFile,
            ref HiddAttributes attributes);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "HidD_GetManufacturerString")]
        public static extern bool HidD_GetManufacturerString(SafeFileHandle hFile,
            StringBuilder buffer, int bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "HidD_GetProductString")]
        public static extern bool HidD_GetProductString(SafeFileHandle hFile,
            StringBuilder buffer, int bufferLength);

        [DllImport("hid.dll", CharSet = CharSet.Auto, SetLastError = true, EntryPoint = "HidD_GetSerialNumberString")]
        public static extern bool HidD_GetSerialNumberString(SafeFileHandle hDevice,
            StringBuilder buffer, int bufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_SetNumInputBuffers(
            SafeFileHandle hidDeviceObject,
            uint numberBuffers);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetPreparsedData(SafeFileHandle hDevice, out SafePreparsedDataHandle preparsedDataHandle);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_FreePreparsedData(IntPtr preparsedDataHandle);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern int HidP_GetCaps(SafePreparsedDataHandle preparsedData, ref HidpCaps capabilities);
    }
}