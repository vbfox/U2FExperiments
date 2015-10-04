using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace U2FExperiments.Win32.Hid
{
    static class HidDll
    {
        [SuppressUnmanagedCodeSecurity]
        public static class NativeMethods
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
        }

        const int ERROR_INVALID_USER_BUFFER = 0x6F8;

        public static Guid HidGuid
        {
            get
            {
                Guid guid;
                NativeMethods.HidD_GetHidGuid(out guid);
                return guid;
            }
        }

        public static HiddAttributes GetAttributes(SafeFileHandle hFile)
        {
            var result = HiddAttributes.Create();
            if (!NativeMethods.HidD_GetAttributes(hFile, ref result))
            {
                throw new Win32Exception();
            }
            return result;
        }

        static string GrowBuffer(Func<StringBuilder, bool> nativeMethod)
        {
            var stringBuilder = new StringBuilder(256);
            while (true)
            {
                if (nativeMethod(stringBuilder))
                {
                    return stringBuilder.ToString();
                }

                var errorCode = Marshal.GetLastWin32Error();
                if (errorCode != ERROR_INVALID_USER_BUFFER)
                {
                    throw new Win32Exception();
                }

                stringBuilder.Capacity = stringBuilder.Capacity * 2;
            }
        }

        public static string GetManufacturerString(SafeFileHandle hFile)
        {
            return GrowBuffer(sb => NativeMethods.HidD_GetManufacturerString(hFile, sb, sb.Capacity));
        }

        public static string GetProductString(SafeFileHandle hFile)
        {
            return GrowBuffer(sb => NativeMethods.HidD_GetProductString(hFile, sb, sb.Capacity));
        }

        public static string GetSerialNumberString(SafeFileHandle hDevice)
        {
            return GrowBuffer(sb => NativeMethods.HidD_GetSerialNumberString(hDevice, sb, sb.Capacity));
        }
    }
}
