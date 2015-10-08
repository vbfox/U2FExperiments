using System.Runtime.InteropServices;

namespace BlackFox.Win32.Hid
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
        public ushort VendorId;
        /* product id */
        public ushort ProductId;
        /* hid vesion number */
        public ushort VersionNumber;

        public static HiddAttributes Create()
        {
            var result = new HiddAttributes();
            result.Size = Marshal.SizeOf(result);
            return result;
        }
    }
}