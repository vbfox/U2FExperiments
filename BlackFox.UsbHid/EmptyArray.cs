namespace BlackFox.UsbHid.Portable
{
    public static class EmptyArray
    {
        private static class Holder<T>
        {
            public static readonly T[] Empty = new T[0];
        }

        public static T[] Of<T>()
        {
            return Holder<T>.Empty;
        }
    }
}