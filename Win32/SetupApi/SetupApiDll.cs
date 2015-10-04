using System;
using System.Runtime.InteropServices;

namespace U2FExperiments.Win32.SetupApi
{
    static class SetupApiDll
    {
        /* function returns a handle to a device information set that contains
         * requested device information elements for a local computer */
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevs(ref Guid gClass,
            [MarshalAs(UnmanagedType.LPStr)] string strEnumerator,
            IntPtr hParent, uint nFlags);

        /* The function enumerates the device interfaces that are contained in 
         * a device information set.*/
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInterfaces(
            IntPtr lpDeviceInfoSet, uint nDeviceInfoData, ref Guid gClass,
            uint nIndex, ref DeviceInterfaceData oInterfaceData);

        /* The SetupDiGetDeviceInterfaceDetail function returns details about 
         * a device interface.*/
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(
            IntPtr lpDeviceInfoSet, ref DeviceInterfaceData oInterfaceData,
            ref DeviceInterfaceDetailData oDetailData,
            uint nDeviceInterfaceDetailDataSize, ref uint nRequiredSize,
            IntPtr lpDeviceInfoData);

        /* destroys device list */
        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr lpInfoSet);
    }
}