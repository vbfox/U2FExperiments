using System;
using System.Threading.Tasks;
using BlackFox.UsbHid.Portable;
using BlackFox.Win32.Hid;
using BlackFox.Win32.Kernel32;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.UsbHid.Win32
{
    public class Win32HidDevice : IHidDevice
    {
        readonly bool ownHandle;
        public SafeFileHandle Handle { get; }

        public Win32HidDeviceInformation Information { get; }
        IHidDeviceInformation IHidDevice.Information => Information;

        public Win32HidDevice([NotNull] string path, [NotNull] SafeFileHandle handle, bool ownHandle)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            this.ownHandle = ownHandle;
            if (handle == null) throw new ArgumentNullException(nameof(handle));

            Handle = handle;
            Information = new Win32HidDeviceInformationLazy(path, handle);
        }

        public HidOutputReport CreateOutputReport(byte id = 0)
        {
            var buffer = new byte[Information.Capabilities.OutputReportByteLength];
            return new HidOutputReport(id, new ArraySegment<byte>(buffer));
        }

        public Task<int> SendOutputReportAsync(HidOutputReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            return Kernel32Dll.WriteFileAsync(Handle, report.GetOutputBuffer());
        }

        public Task<HidInputReport> GetInputReportAsync()
        {
            return Kernel32Dll.ReadFileAsync<byte>(Handle, Information.Capabilities.InputReportByteLength)
                .ContinueWith(task => new HidInputReport(task.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public void SetNumInputBuffers(uint numberBuffers)
        {
            HidDll.SetNumInputBuffers(Handle, numberBuffers);
        }

        private static SafeFileHandle OpenHandle(string path, Kernel32FileAccess access, bool throwOnError)
        {
            return Kernel32Dll.CreateFile(
                path,
                access,
                FileShareMode.Read | FileShareMode.Write,
                IntPtr.Zero,
                FileCreationDisposition.OpenExisiting,
                FileFlags.Overlapped,
                IntPtr.Zero,
                throwOnError);
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

        public static Win32HidDevice FromPath(string path, Kernel32FileAccess accessMode)
        {
            var handle = OpenHandle(path, accessMode, true);
            return new Win32HidDevice(path, handle, true);
        }

        public static Win32HidDevice TryFromPath(string path, Kernel32FileAccess accessMode)
        {
            var handle = OpenHandle(path, accessMode, false);
            return handle.IsInvalid ? null : new Win32HidDevice(path, handle, true);
        }
    }
}
