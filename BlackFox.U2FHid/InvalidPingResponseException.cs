using System;

namespace BlackFox.U2FHid
{
    public class InvalidPingResponseException : Exception
    {
        public InvalidPingResponseException(string message)
            : base(message)
        {
            
        }
    }
}