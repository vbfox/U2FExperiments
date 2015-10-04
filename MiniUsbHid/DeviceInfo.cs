namespace U2FExperiments.MiniUsbHid
{
    public class DeviceInfo
    {
        public string Path { get; private set; }
        public bool CanBeOpened { get; }
        public short Vid { get; private set; }
        public short Pid { get; private set; }
        public string Product { get; private set; }
        public string Manufacturer { get; private set; }

        public DeviceInfo(string path, bool canBeOpened, string product, string manufacturer,
            short vid, short pid)
        {
            Path = path;
            CanBeOpened = canBeOpened;
            Product = product;
            Manufacturer = manufacturer;
            Vid = vid;
            Pid = pid;
        }
    }
}
