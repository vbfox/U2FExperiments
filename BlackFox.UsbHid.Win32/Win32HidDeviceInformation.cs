using System.Diagnostics;
using System.Threading.Tasks;
using BlackFox.Win32.Hid;

namespace BlackFox.UsbHid.Win32
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class Win32HidDeviceInformation : IHidDeviceInformation
    {
        public abstract string Path { get; }
        public abstract HidpCaps Capabilities { get; }
        public abstract ushort ProductId { get; }
        public abstract ushort VendorId { get; }
        public abstract ushort Version { get; }
        public abstract string Manufacturer { get; }
        public abstract string Product { get; }
        public abstract string SerialNumber { get; }

        Task<IHidDevice> IHidDeviceInformation.OpenDeviceAsync(HidDeviceAccessMode accessMode)
        {
            return Win32HidDeviceFactory.Instance.FromIdAsyncInferface(Path, accessMode, this);
        }

        public Task<Win32HidDevice> OpenDeviceAsync(HidDeviceAccessMode accessMode = HidDeviceAccessMode.ReadWrite)
        {
            return Win32HidDeviceFactory.Instance.FromIdAsync(Path, accessMode, this);
        }

        string IHidDeviceInformation.Id => Path;
        ushort IHidDeviceInformation.UsageId => Capabilities.Usage;
        ushort IHidDeviceInformation.UsagePage => Capabilities.UsagePage;

        internal abstract string DebuggerDisplay { get; }
    }
}