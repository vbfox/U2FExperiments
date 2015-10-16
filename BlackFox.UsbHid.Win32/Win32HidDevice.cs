using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BlackFox.Binary;
using BlackFox.Win32.Hid;
using BlackFox.Win32.Kernel32;
using Common.Logging;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.UsbHid.Win32
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Win32HidDevice : IHidDevice
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Win32HidDevice));

        readonly bool ownHandle;
        public SafeFileHandle Handle { get; }

        [NotNull]
        public Win32HidDeviceInformation Information { get; }

        IHidDeviceInformation IHidDevice.Information => Information;

        public Win32HidDevice([NotNull] string path, [NotNull] SafeFileHandle handle, bool ownHandle)
            :this(path, handle, ownHandle, null)
        {
        }

        internal Win32HidDevice([NotNull] string path, [NotNull] SafeFileHandle handle, bool ownHandle,
            [CanBeNull] Win32HidDeviceInformation knownInformation)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (handle == null) throw new ArgumentNullException(nameof(handle));

            this.ownHandle = ownHandle;
            Handle = handle;
            Information = knownInformation ?? new Win32HidDeviceInformationLazy(path, handle);
        }

        public HidOutputReport CreateOutputReport(byte id = 0)
        {
            var buffer = new byte[Information.Capabilities.OutputReportByteLength];
            return new HidOutputReport(id, new ArraySegment<byte>(buffer));
        }

        public Task<int> SendOutputReportAsync(HidOutputReport report)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));

            var outputBuffer = report.GetOutputBuffer();
            log.Trace("Sending output report:\r\n\r\n " + outputBuffer.ToHexString());
            log.Trace(outputBuffer.ToLoggableAsHex("Sending output report:"));

            return Kernel32Dll.WriteFileAsync(Handle, outputBuffer)
                .LogFaulted(log, "Sending output report failed");
        }

        public Task<HidInputReport> GetInputReportAsync()
        {
            return Kernel32Dll.ReadFileAsync<byte>(Handle, Information.Capabilities.InputReportByteLength)
                .ContinueWith(task => OnInputReportRead(task), TaskContinuationOptions.OnlyOnRanToCompletion)
                .LogFaulted(log, "Receiving input report failed");
        }

        static HidInputReport OnInputReportRead(Task<ArraySegment<byte>> task)
        {
            log.Trace(task.Result.ToLoggableAsHex("Received input report:"));
            return new HidInputReport(task.Result);
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

        internal static Win32HidDevice FromPath([NotNull] string path, Kernel32FileAccess accessMode,
            [CanBeNull] Win32HidDeviceInformation knownInformation)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var handle = OpenHandle(path, accessMode, true);
            return new Win32HidDevice(path, handle, true, knownInformation);
        }

        public static Win32HidDevice FromPath(string path, Kernel32FileAccess accessMode)
        {
            return FromPath(path, accessMode, null);
        }

        public static Win32HidDevice TryFromPath(string path, Kernel32FileAccess accessMode)
        {
            var handle = OpenHandle(path, accessMode, false);
            return handle.IsInvalid ? null : new Win32HidDevice(path, handle, true);
        }

        private string DebuggerDisplay => $"Win32 HID {Information.DebuggerDisplay}";
    }
}
