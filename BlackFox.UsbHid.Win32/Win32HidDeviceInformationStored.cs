using System;
using BlackFox.Win32.Hid;
using JetBrains.Annotations;

namespace BlackFox.UsbHid.Win32
{
    internal class Win32HidDeviceInformationStored : Win32HidDeviceInformation
    {
        public override string Path { get; }
        public override HidpCaps Capabilities { get; }
        public override string SerialNumber { get; }
        public override ushort ProductId { get; }
        public override ushort VendorId { get; }
        public override ushort Version { get; }
        public override string Manufacturer { get; }
        public override string Product { get; }

        public Win32HidDeviceInformationStored([NotNull] Win32HidDeviceInformation other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            Path = other.Path;
            Capabilities = other.Capabilities;
            SerialNumber = other.SerialNumber;
            ProductId = other.ProductId;
            VendorId = other.VendorId;
            Version = other.Version;
            Manufacturer = other.Manufacturer;
            Product = other.Product;
        }
    }
}