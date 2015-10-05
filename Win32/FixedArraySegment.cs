using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace U2FExperiments.Win32
{
    class FixedArraySegment<T> : IDisposable
        where T : struct 
    {
        GCHandle? handle;

        public IntPtr Pointer { get; }
        public int Size { get; }

        public FixedArraySegment(ArraySegment<T>? segment)
        {
            if (segment == null)
            {
                Pointer = IntPtr.Zero;
                Size = 0;
                return;
            }

            handle = GCHandle.Alloc(segment.Value.Array, GCHandleType.Pinned);
            var arrayPtr = handle.Value.AddrOfPinnedObject();
            var elementSize = Marshal.SizeOf(typeof(T));
            Pointer = new IntPtr(arrayPtr.ToInt64() + elementSize * segment.Value.Offset);
            Size = elementSize*segment.Value.Count;
        }

        public void Dispose()
        {
            handle?.Free();
        }
    }
}
