using System;

namespace BlackFox.Binary
{
    internal static class EmptyArraySegment<T>
    {
        public static ArraySegment<T> Value { get; } = new ArraySegment<T>(EmptyArray<T>.Value);
    }
}