﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.Win32.Hid
{
    public static class HidDll
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

        const int ERROR_INVALID_USER_BUFFER = 0x6F8;

        const int HIDP_STATUS_SUCCESS = 0x00110000;
        const int HIDP_STATUS_INVALID_PREPARSED_DATA = unchecked ((int)0xC0110001);

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

        static string GrowBufferOrNull(Func<StringBuilder, bool> nativeMethod)
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
                    return null;
                }

                stringBuilder.Capacity = stringBuilder.Capacity * 2;
            }
        }

        public static string GetManufacturerString(SafeFileHandle hFile)
        {
            return GrowBufferOrNull(sb => NativeMethods.HidD_GetManufacturerString(hFile, sb, sb.Capacity));
        }

        public static string GetProductString(SafeFileHandle hFile)
        {
            return GrowBufferOrNull(sb => NativeMethods.HidD_GetProductString(hFile, sb, sb.Capacity));
        }

        public static string GetSerialNumberString(SafeFileHandle hDevice)
        {
            return GrowBufferOrNull(sb => NativeMethods.HidD_GetSerialNumberString(hDevice, sb, sb.Capacity));
        }

        public static void SetNumInputBuffers(SafeFileHandle hidDeviceObject, uint numberBuffers)
        {
            if (!NativeMethods.HidD_SetNumInputBuffers(hidDeviceObject, numberBuffers))
            {
                throw new Win32Exception();
            }
        }

        public static SafePreparsedDataHandle GetPreparsedData(SafeFileHandle hDevice)
        {
            SafePreparsedDataHandle preparsedDataHandle;
            if (!NativeMethods.HidD_GetPreparsedData(hDevice, out preparsedDataHandle))
            {
                throw new Win32Exception();
            }

            return preparsedDataHandle;
        }

        public static HidpCaps GetCaps(SafePreparsedDataHandle preparsedData)
        {
            var hidCaps = new HidpCaps();
            var result = NativeMethods.HidP_GetCaps(preparsedData, ref hidCaps);
            switch (result)
            {
                case HIDP_STATUS_SUCCESS:
                    return hidCaps;

                case HIDP_STATUS_INVALID_PREPARSED_DATA:
                    throw new Win32Exception(result, "Invalid Preparsed Data");

                default:
                    throw new Win32Exception(result);
            }
        }
    }
}