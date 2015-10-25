using System;
using System.Runtime.InteropServices;

namespace BlackFox.Win32
{
    class FixedArraySegment<T> : IDisposable
        where T : struct
    {
        GCHandle? handle;

        public IntPtr Pointer { get; }
        public long LongSizeInBytes { get; }
        public int ElementSize { get; }

        public int SizeInBytes
        {
            get
            {
                if (LongSizeInBytes > int.MaxValue || LongSizeInBytes < int.MinValue)
                {
                    throw new InvalidOperationException("Segment is too big for it's size to be represented by an Int32");
                }

                return unchecked ((int)LongSizeInBytes);
            }
        }

        public FixedArraySegment(ArraySegment<T>? maybeSegment)
        {
            if (maybeSegment == null)
            {
                Pointer = IntPtr.Zero;
                LongSizeInBytes = 0;
                ElementSize = 0;
                return;
            }

            var segment = maybeSegment.Value;
            handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);
            var arrayPtr = handle.Value.AddrOfPinnedObject();
            ElementSize = Marshal.SizeOf(typeof (T));
            Pointer = new IntPtr(arrayPtr.ToInt64() + ElementSize*segment.Offset);
            LongSizeInBytes = ElementSize*segment.Count;
        }

        public void Dispose()
        {
            handle?.Free();
        }
    }
}
