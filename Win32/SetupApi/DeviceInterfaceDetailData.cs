using System.Runtime.InteropServices;

namespace U2FExperiments.Win32.SetupApi
{
    /* A structure contains the path for a device interface.*/
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DeviceInterfaceDetailData
    {
        /* size of fixed part of structure */
        public int Size;
        /* device path, as to be used by CreateFile */
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public string DevicePath;
    }
}