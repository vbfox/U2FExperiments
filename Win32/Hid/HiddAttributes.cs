using System.Runtime.InteropServices;

namespace U2FExperiments.Win32.Hid
{
    /// <summary>
    /// The HIDD_ATTRIBUTES structure contains vendor information about a HIDClass device.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HiddAttributes
    {
        /* size in bytes */
        public int Size;
        /* vendor id */
        public short VendorID;
        /* product id */
        public short ProductID;
        /* hid vesion number */
        public short VersionNumber;
    }
}