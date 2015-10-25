using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.Win32.Hid
{
    public static class HidDll
    {
        const int ERROR_INVALID_USER_BUFFER = 0x6F8;

        const int HIDP_STATUS_SUCCESS = 0x00110000;
        const int HIDP_STATUS_INVALID_PREPARSED_DATA = unchecked ((int)0xC0110001);

        public static Guid HidGuid
        {
            get
            {
                Guid guid;
                HidDllNativeMethods.HidD_GetHidGuid(out guid);
                return guid;
            }
        }

        public static HiddAttributes GetAttributes(SafeFileHandle hFile)
        {
            var result = HiddAttributes.Create();
            if (!HidDllNativeMethods.HidD_GetAttributes(hFile, ref result))
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
            return GrowBufferOrNull(sb => HidDllNativeMethods.HidD_GetManufacturerString(hFile, sb, sb.Capacity));
        }

        public static string GetProductString(SafeFileHandle hFile)
        {
            return GrowBufferOrNull(sb => HidDllNativeMethods.HidD_GetProductString(hFile, sb, sb.Capacity));
        }

        public static string GetSerialNumberString(SafeFileHandle hDevice)
        {
            return GrowBufferOrNull(sb => HidDllNativeMethods.HidD_GetSerialNumberString(hDevice, sb, sb.Capacity));
        }

        public static void SetNumInputBuffers(SafeFileHandle hidDeviceObject, uint numberBuffers)
        {
            if (!HidDllNativeMethods.HidD_SetNumInputBuffers(hidDeviceObject, numberBuffers))
            {
                throw new Win32Exception();
            }
        }

        public static SafePreparsedDataHandle GetPreparsedData(SafeFileHandle hDevice)
        {
            SafePreparsedDataHandle preparsedDataHandle;
            if (!HidDllNativeMethods.HidD_GetPreparsedData(hDevice, out preparsedDataHandle))
            {
                throw new Win32Exception();
            }

            return preparsedDataHandle;
        }

        public static HidpCaps GetCaps(SafePreparsedDataHandle preparsedData)
        {
            var hidCaps = new HidpCaps();
            var result = HidDllNativeMethods.HidP_GetCaps(preparsedData, ref hidCaps);
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
