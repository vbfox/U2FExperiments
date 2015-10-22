using System;

namespace BlackFox.UsbHid
{
    /// <summary>
    /// Base class for all common exceptions shared by USB Hid devices
    /// </summary>
    public class UsbHidException : Exception
    {
        public UsbHidException()
        {
        }

        public UsbHidException(string message) : base(message)
        {
        }

        public UsbHidException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
