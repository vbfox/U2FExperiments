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
        public short VendorId;
        /* product id */
        public short ProductId;
        /* hid vesion number */
        public short VersionNumber;

        public static HiddAttributes Create()
        {
            var result = new HiddAttributes();
            result.Size = Marshal.SizeOf(result);
            return result;
        }
    }
}