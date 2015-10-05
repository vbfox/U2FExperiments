using System;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;
using U2FExperiments.Win32;
using U2FExperiments.Win32.Hid;
using U2FExperiments.Win32.Kernel32;

namespace U2FExperiments.MiniUsbHid
{
    public class Device : IDisposable
    {
        readonly bool ownHandle;
        public SafeFileHandle Handle { get; set; }

        public Device([NotNull] SafeFileHandle handle, bool ownHandle)
        {
            this.ownHandle = ownHandle;
            if (handle == null) throw new ArgumentNullException(nameof(handle));

            Handle = handle;
        }

        public string GetManufacturer() => HidDll.GetManufacturerString(Handle);
        public string GetProduct() => HidDll.GetProductString(Handle);
        public string GetSerialNumber() => HidDll.GetSerialNumberString(Handle);
        public HiddAttributes GetAttributes() => HidDll.GetAttributes(Handle);

        public HidpCaps GetCaps()
        {
            using (var preparsedData = HidDll.GetPreparsedData(Handle))
            {
                return HidDll.GetCaps(preparsedData);
            }
        }

        public void SetNumInputBuffers(uint numberBuffers)
        {
            HidDll.SetNumInputBuffers(Handle, numberBuffers);
        }

        private static SafeFileHandle OpenReadHandle(string path)
        {
            return Kernel32Dll.NativeMethods.CreateFile(path,
                Native.GENERIC_READ,
                Native.FILE_SHARE_READ,
                IntPtr.Zero, Native.OPEN_EXISTING, Native.FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);
        }

        public static Device OpenRead(string path)
        {
            return new Device(OpenReadHandle(path), true);
        }

        private static SafeFileHandle OpenHandle(string path)
        {
            return Kernel32Dll.CreateFile(path,
                Native.GENERIC_READ | Native.GENERIC_WRITE,
                Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE,
                IntPtr.Zero, Native.OPEN_EXISTING, Native.FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);
        }

        public static Device Open(string path)
        {
            return new Device(OpenHandle(path), true);
        }

        public void Dispose()
        {
            if (ownHandle)
            {
                Handle.Dispose();
            }
        }
    }
}
