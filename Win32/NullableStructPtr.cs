using System;
using System.Runtime.InteropServices;

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

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Handle);
            }
        }
    }
}