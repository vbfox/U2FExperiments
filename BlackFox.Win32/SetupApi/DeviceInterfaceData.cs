using System;
using System.Runtime.InteropServices;

namespace BlackFox.Win32.SetupApi
{
    /* structure returned by SetupDiEnumDeviceInterfaces */
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DeviceInterfaceData
    {
        /* size of fixed part of structure */
        public int Size;
        /* The GUID for the class to which the device interface belongs. */
        public Guid InterfaceClassGuid;
        /* Can be one or more of the following: SPINT_ACTIVE, 
         * SPINT_DEFAULT, SPINT_REMOVED */
        public int Flags;
        /* do not use */
        public IntPtr Reserved;

        public static DeviceInterfaceData Create()
        {
            var result = new DeviceInterfaceData();
            result.Size = Marshal.SizeOf(result);
            return result;
        }
    }
}