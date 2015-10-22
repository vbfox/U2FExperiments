using System;
using System.ComponentModel;

namespace BlackFox.UsbHid.Win32
{
    internal static class ExceptionConversion
    {
        // ERROR_DEVICE_NOT_CONNECTED = 1167 (0x48F) : The device is not connected.
        private const int ErrorDeviceNotConnected = 0x48F;

        public static Exception ConvertException(Win32Exception exception)
        {
            switch (exception.NativeErrorCode)
            {
                case ErrorDeviceNotConnected:
                    throw new DeviceNotConnectedException("The HID device isn't connected", exception);
            }
            return new UsbHidException("Unknown HID device error: " + exception.Message, exception);
        }
    }
}