using System;

namespace BlackFox.UsbHid
{
    public static class EmptyArraySegment
    {
        private static class Holder<T>
        {
            public static readonly ArraySegment<T> Empty = new ArraySegment<T>(EmptyArray.Of<T>());
        }

        public static ArraySegment<T> Of<T>()
        {
            return Holder<T>.Empty;
        }
    }
}