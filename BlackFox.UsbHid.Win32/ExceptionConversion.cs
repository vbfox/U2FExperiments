using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace BlackFox.UsbHid.Win32
{
    internal static class ExceptionConversion
    {
        // ERROR_DEVICE_NOT_CONNECTED = 1167 (0x48F) : The device is not connected.
        private const int ErrorDeviceNotConnected = 0x48F;
        private static readonly int errorDeviceNotConnectedHResult = HRESULT_FROM_WIN32(ErrorDeviceNotConnected);

        public static Exception ConvertException(Exception exception)
        {
            var win32Exception = exception as Win32Exception;
            if (win32Exception?.NativeErrorCode == ErrorDeviceNotConnected)
            {
                throw new DeviceNotConnectedException("The HID device isn't connected", exception);
            }

            var comException = exception as COMException;
            if (comException?.ErrorCode == errorDeviceNotConnectedHResult)
            {
                throw new DeviceNotConnectedException("The HID device isn't connected", exception);
            }

            return new UsbHidException("Unknown HID device error: " + exception.Message, exception);
        }

        const int FacilityWin32 = 7;

        static int HRESULT_FROM_WIN32(int win32Error)
        {
            var hr = win32Error <= 0
             ? (uint)(win32Error)
             : (((uint)(win32Error & 0x0000FFFF) | (FacilityWin32 << 16) | 0x80000000));
            return (int)hr;
        }
    }
}