using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using JetBrains.Annotations;

namespace BlackFox.UsbHid.Uwp
{
    public class UwpHidDeviceInformation : IHidDeviceInformation
    {
        readonly UwpHidDeviceFactory factory;
        readonly DeviceInformation deviceInformation;
        public string Id => deviceInformation.Id;
        public ushort ProductId => UInt16Property(UwpDevicePropertyNames.ProductId);
        public ushort VendorId => UInt16Property(UwpDevicePropertyNames.VendorId);
        public ushort Version => 0;
        public ushort UsageId => UInt16Property(UwpDevicePropertyNames.UsageId);
        public ushort UsagePage => UInt16Property(UwpDevicePropertyNames.UsagePage);
        public string Manufacturer => "";
        public string Product => deviceInformation.Name;
        public string SerialNumber => "";

        public Task<IHidDevice> OpenDeviceAsync(HidDeviceAccessMode accessMode = HidDeviceAccessMode.ReadWrite,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return factory.FromIdAsync(Id, accessMode, cancellationToken);
        }

        public UwpHidDeviceInformation([NotNull] UwpHidDeviceFactory factory, [NotNull] DeviceInformation deviceInformation)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            if (deviceInformation == null)
            {
                throw new ArgumentNullException(nameof(deviceInformation));
            }

            this.factory = factory;
            this.deviceInformation = deviceInformation;
        }

        string StringProperty(string name)
        {
            object result;
            deviceInformation.Properties.TryGetValue(name, out result);
            return (string)result ?? "";
        }

        ushort UInt16Property(string name)
        {
            object result;
            deviceInformation.Properties.TryGetValue(name, out result);
            return (ushort)result;
        }
    }
}