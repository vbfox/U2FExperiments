using System;
using BlackFox.Win32.Hid;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace BlackFox.UsbHid.Win32
{
    internal class Win32HidDeviceInformationLazy : Win32HidDeviceInformation
    {
        public override string Path { get;  }
        private readonly SafeFileHandle handle;
        public override HidpCaps Capabilities => capabilities.Value;
        public override ushort ProductId => attributes.Value.ProductId;
        public override ushort VendorId => attributes.Value.VendorId;
        public override ushort Version => attributes.Value.VersionNumber;
        public override string Manufacturer => manufacturer.Value;
        public override string Product => product.Value;
        public override string SerialNumber => serialNumber.Value;

        private readonly Lazy<HiddAttributes> attributes;
        private readonly Lazy<HidpCaps> capabilities;
        private readonly Lazy<string> manufacturer;
        private readonly Lazy<string> serialNumber;
        private readonly Lazy<string> product;

        internal Win32HidDeviceInformationLazy(string path, [NotNull] SafeFileHandle handle)
        {
            Path = path;
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            if (handle.IsInvalid) throw new ArgumentException("Handle is invalid", nameof(handle));

            this.handle = handle;

            manufacturer = new Lazy<string>(() => HidDll.GetManufacturerString(handle));
            product = new Lazy<string>(() => HidDll.GetProductString(handle));
            serialNumber = new Lazy<string>(() => HidDll.GetSerialNumberString(handle));
            attributes = new Lazy<HiddAttributes>(() => HidDll.GetAttributes(handle));
            capabilities = new Lazy<HidpCaps>(GetCapabilities);
        }

        private HidpCaps GetCapabilities()
        {
            using (var preparsedData = HidDll.GetPreparsedData(handle))
            {
                return HidDll.GetCaps(preparsedData);
            }
        }
    }
}