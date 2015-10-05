using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;
using U2FExperiments.Win32;
using U2FExperiments.Win32.Hid;
using U2FExperiments.Win32.Kernel32;

namespace U2FExperiments.MiniUsbHid
{
    public class HidDevice : IDisposable
    {
        readonly bool ownHandle;
        public SafeFileHandle Handle { get; set; }

        public HidDevice([NotNull] SafeFileHandle handle, bool ownHandle)
        {
            this.ownHandle = ownHandle;
            if (handle == null) throw new ArgumentNullException(nameof(handle));

            Handle = handle;

            attributes = new Lazy<HiddAttributes>(() => HidDll.GetAttributes(Handle));
        }

        readonly Lazy<HiddAttributes> attributes;

        public ushort ProductId => attributes.Value.ProductId;
        public ushort VendorId => attributes.Value.VendorId;
        public ushort Version => attributes.Value.VersionNumber;

        public string GetManufacturer() => HidDll.GetManufacturerString(Handle);
        public string GetProduct() => HidDll.GetProductString(Handle);
        public string GetSerialNumber() => HidDll.GetSerialNumberString(Handle);

        public HidOutputReport CreateOutputReport()
        {
            var buffer = new byte[GetCaps().OutputReportByteLength];
            return new HidOutputReport(0, new ArraySegment<byte>(buffer));
        }

        public Task<int> SendOutputReportAsync([NotNull] HidOutputReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            return Kernel32Dll.WriteFileAsync(Handle, report.GetOutputBuffer());
        }

        public Task<HidInputReport> GetInputReportAsync()
        {
            return Kernel32Dll.ReadFileAsync<byte>(Handle, GetCaps().InputReportByteLength)
                .ContinueWith(task => new HidInputReport(task.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

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

        public static HidDevice OpenRead(string path)
        {
            return new HidDevice(OpenReadHandle(path), true);
        }

        private static SafeFileHandle OpenHandle(string path)
        {
            return Kernel32Dll.CreateFile(path,
                Native.GENERIC_READ | Native.GENERIC_WRITE,
                Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE,
                IntPtr.Zero, Native.OPEN_EXISTING, Native.FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);
        }

        public static HidDevice Open(string path)
        {
            return new HidDevice(OpenHandle(path), true);
        }

        public void Close()
        {
            ((IDisposable)this).Dispose();
        }

        void IDisposable.Dispose()
        {
            if (ownHandle)
            {
                Handle.Dispose();
            }
        }
    }
}
