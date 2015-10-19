using System;

namespace BlackFox.U2FHid
{
    public class DeviceNotInitializedException : Exception
    {
        public DeviceNotInitializedException()
        {
        }

        public DeviceNotInitializedException(string message) : base(message)
        {
        }

        public DeviceNotInitializedException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}