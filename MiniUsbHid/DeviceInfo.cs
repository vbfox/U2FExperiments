using U2FExperiments.Win32.Hid;

namespace U2FExperiments.MiniUsbHid
{
    public class DeviceInfo
    {
        public string Path { get; }
        public bool CanBeOpened { get; }
        public ushort VendorId { get; }
        public ushort ProductId { get; }
        public ushort VersionNumber { get; }
        public HidpCaps? Capabilities { get; set; }
        public string Product { get; }
        public string Manufacturer { get; }
        public string SerialNumber { get; }

        public DeviceInfo(string path, bool canBeOpened, string product, string manufacturer, string serialNumber, ushort vendorId, ushort productId, ushort versionNumber, HidpCaps? capabilities)
        {
            Path = path;
            CanBeOpened = canBeOpened;
            Product = product;
            Manufacturer = manufacturer;
            VendorId = vendorId;
            ProductId = productId;
            VersionNumber = versionNumber;
            Capabilities = capabilities;
            SerialNumber = serialNumber;
        }

        public HidDevice OpenDevice()
        {
            return HidDevice.Open(Path);
        }
    }
}
