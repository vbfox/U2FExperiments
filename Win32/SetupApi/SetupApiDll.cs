using System;
using System.Runtime.InteropServices;

namespace U2FExperiments.Win32.SetupApi
{
    static class SetupApiDll
    {
        /* function returns a handle to a device information set that contains
         * requested device information elements for a local computer */
        [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetClassDevs")]
        public static extern DeviceInfoListSafeHandle GetClassDevs(ref Guid gClass,
            [MarshalAs(UnmanagedType.LPStr)] string strEnumerator,
            IntPtr hParent, uint nFlags);

        /* The function enumerates the device interfaces that are contained in 
         * a device information set.*/
        [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiEnumDeviceInterfaces")]
        public static extern bool EnumDeviceInterfaces(
            DeviceInfoListSafeHandle lpDeviceInfoSet, uint nDeviceInfoData, ref Guid gClass,
            uint nIndex, ref DeviceInterfaceData oInterfaceData);

        /* The SetupDiGetDeviceInterfaceDetail function returns details about 
         * a device interface.*/
        [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceInterfaceDetail")]
        public static extern bool GetDeviceInterfaceDetail(
            DeviceInfoListSafeHandle lpDeviceInfoSet, ref DeviceInterfaceData oInterfaceData,
            ref DeviceInterfaceDetailData oDetailData,
            uint nDeviceInterfaceDetailDataSize, ref uint nRequiredSize,
            IntPtr lpDeviceInfoData);

        /* destroys device list */
        [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiDestroyDeviceInfoList")]
        public static extern bool DestroyDeviceInfoList(IntPtr lpInfoSet);
    }
}