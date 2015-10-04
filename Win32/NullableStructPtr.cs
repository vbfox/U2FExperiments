using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace U2FExperiments.Win32
{
    class NullableStructPtr<T> : IDisposable
        where T : struct
    {
        public IntPtr Handle { get; }

        public NullableStructPtr(T? value)
        {
            if (value != null)
            {
                Handle = Marshal.AllocHGlobal(Marshal.SizeOf(value.Value));
                Marshal.StructureToPtr(value.Value, Handle, false);
            }
            else
            {
                Handle = IntPtr.Zero;
            }
        }

        bool HasValue => Handle != IntPtr.Zero;

        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException("No value");
                }

                return (T)Marshal.PtrToStructure(Handle, typeof(T));
            }
        }

        public void Dispose()
        {
            if (HasValue)
            {
                Marshal.FreeHGlobal(Handle);
            }
        }
    }
}