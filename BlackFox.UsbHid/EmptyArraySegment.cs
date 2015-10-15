using System;

namespace BlackFox.UsbHid
{
    public static class EmptyArraySegment<T>
    {
        public static ArraySegment<T> Value { get; } = new ArraySegment<T>(EmptyArray<T>.Value);
    }
}