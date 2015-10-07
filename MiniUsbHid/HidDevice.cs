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

        private static SafeFileHandle OpenInfoHandle(string path, bool throwOnError)
        {
            return Kernel32Dll.CreateFile(path,
                FileAccess.None,
                FileShareMode.Read | FileShareMode.Write,
                IntPtr.Zero,
                FileCreationDisposition.OpenExisiting, 
                FileFlags.Overlapped, 
                IntPtr.Zero,
                throwOnError);
        }

        public static HidDevice OpenForInfo(string path)
        {
            return new HidDevice(OpenInfoHandle(path, true), true);
        }

        public static HidDevice TryOpenForInfo(string path)
        {
            var handle = OpenInfoHandle(path, false);
            return handle.IsInvalid ? null : new HidDevice(handle, true);
        }

        private static SafeFileHandle OpenHandle(string path, bool throwOnError)
        {
            return Kernel32Dll.CreateFile(
                path,
                FileAccess.GenericRead | FileAccess.GenericWrite,
                FileShareMode.Read | FileShareMode.Write,
                IntPtr.Zero,
                FileCreationDisposition.OpenExisiting,
                FileFlags.Overlapped,
                IntPtr.Zero,
                throwOnError);
        }

        public static HidDevice Open(string path)
        {
            return new HidDevice(OpenHandle(path, true), true);
        }

        public static HidDevice TryOpen(string path)
        {
            var handle = OpenHandle(path, false);
            return handle.IsInvalid ? null : new HidDevice(handle, true);
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
