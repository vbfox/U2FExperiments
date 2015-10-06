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
        public string Product { get; }
        public string Manufacturer { get; }

        public DeviceInfo(string path, bool canBeOpened, string product, string manufacturer,
            ushort vendorId, ushort productId, ushort versionNumber)
        {
            Path = path;
            CanBeOpened = canBeOpened;
            Product = product;
            Manufacturer = manufacturer;
            VendorId = vendorId;
            ProductId = productId;
            VersionNumber = versionNumber;
        }

        public HidDevice OpenDevice()
        {
            return HidDevice.Open(Path);
        }
    }
}
