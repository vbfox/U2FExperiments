using System;

namespace BlackFox.UsbHid
{
    /// <summary>
    /// The device isn't connected anymore
    /// </summary>
    public class DeviceNotConnectedException : UsbHidException
    {
        public DeviceNotConnectedException()
        {
        }

        public DeviceNotConnectedException(string message) : base(message)
        {
        }

        public DeviceNotConnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}