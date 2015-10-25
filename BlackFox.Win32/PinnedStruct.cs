using System;
using System.Runtime.InteropServices;

namespace BlackFox.Win32
{
    static class PinnedStructExtensions
    {
        public static PinnedStruct<T> Pin<T>(this T value)
            where T : struct
        {
            return new PinnedStruct<T>(value);
        }

        public static PinnedStruct<T> Pin<T>(this T? value)
            where T : struct
        {
            return new PinnedStruct<T>(value);
        }
    }

    /// <summary>
    /// Allocate a structure on the heap and pin it in place, allowing a controlled release (Via Dispose)
    /// </summary>
    class PinnedStruct<T> : IDisposable
        where T : struct
    {
        GCHandle? gcHandle;

        public IntPtr IntPtr
        {
            get
            {
                ThrowIfDisposed();
                return intPtr;
            }
        }

        public unsafe void* Pointer
        {
            get
            {
                ThrowIfDisposed();
                return IntPtr.ToPointer();
            }
        }

        public PinnedStruct(T? value)
        {
            if (value.HasValue)
            {
                object box = value.Value;
                gcHandle = GCHandle.Alloc(box, GCHandleType.Pinned);
                intPtr = gcHandle.Value.AddrOfPinnedObject();
            }
            else
            {
                gcHandle = null;
                intPtr = IntPtr.Zero;
            }
            disposed = false;
        }

        public bool HasValue
        {
            get
            {
                ThrowIfDisposed();
                return gcHandle.HasValue;
            }
        }

        public T Value
        {
            get
            {
                ThrowIfDisposed();

                if (!gcHandle.HasValue)
                {
                    throw new InvalidOperationException("The PinnedStruct was created without a value");
                }

                return (T)gcHandle.Value.Target;
            }
        }

        void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("PinnedStruct");
            }
        }

        bool disposed;
        readonly IntPtr intPtr;

        public void Dispose()
        {
            if (gcHandle.HasValue && !disposed)
            {
                gcHandle.Value.Free();
            }
            disposed = true;
        }
    }
}
