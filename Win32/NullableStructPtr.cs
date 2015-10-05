using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace U2FExperiments.Win32
{
    static class NullableStructPtrExtensions
    {
        public static NullableStructPtr<T> ToPtr<T>(this T value)
            where T : struct
        {
            return new NullableStructPtr<T>(value);
        }

        public static NullableStructPtr<T> ToPtr<T>(this T? value)
            where T : struct
        {
            return new NullableStructPtr<T>(value);
        }
    }

    class NullableStructPtr<T> : IDisposable
        where T : struct
    {
        public IntPtr Pointer { get; }

        public NullableStructPtr(T? value)
        {
            if (value != null)
            {
                Pointer = Marshal.AllocHGlobal(Marshal.SizeOf(value.Value));
                Marshal.StructureToPtr(value.Value, Pointer, false);
            }
            else
            {
                Pointer = IntPtr.Zero;
            }
        }

        bool HasValue => Pointer != IntPtr.Zero;

        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException("No value");
                }

                return (T)Marshal.PtrToStructure(Pointer, typeof(T));
            }
        }

        public void Dispose()
        {
            if (HasValue)
            {
                Marshal.FreeHGlobal(Pointer);
            }
        }
    }
}