using System;

namespace BlackFox.U2F.Gnubby
{
    /// <summary>
    /// The key is gone, not connected anymore
    /// </summary>
    public class KeyGoneException : Exception
    {
        public KeyGoneException(string message) : base(message)
        {
        }

        public KeyGoneException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}