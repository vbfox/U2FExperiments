using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace U2FExperiments.Win32.SetupApi
{
    static partial class SetupApiDll
    {
        /* function returns a handle to a device information set that contains
         * requested device information elements for a local computer */
        public static DeviceInfoListSafeHandle GetClassDevs(Guid? gClass,
            [MarshalAs(UnmanagedType.LPStr)] string strEnumerator,
            IntPtr hParent, GetClassDevsFlags nFlags)
        {
            using (var guidPtr = new NullableStructPtr<Guid>(gClass))
            {
                return NativeMethods.SetupDiGetClassDevs(guidPtr.Handle, strEnumerator, hParent, nFlags);
            }
        }

        /* The function enumerates the device interfaces that are contained in 
         * a device information set.*/

        public static bool EnumDeviceInterfaces(
            DeviceInfoListSafeHandle lpDeviceInfoSet,
            uint nDeviceInfoData,
            Guid gClass,
            uint nIndex,
            ref DeviceInterfaceData oInterfaceData)
        {
            return NativeMethods.SetupDiEnumDeviceInterfaces(lpDeviceInfoSet, nDeviceInfoData, ref gClass, nIndex,
                ref oInterfaceData);
        }

        const int ERROR_NO_MORE_ITEMS = 259;

        public static IEnumerable<DeviceInterfaceData> EnumDeviceInterfaces(
            DeviceInfoListSafeHandle lpDeviceInfoSet,
            uint nDeviceInfoData,
            Guid gClass)
        {
            uint index = 0;
            while (true)
            {
                var data = new DeviceInterfaceData();
                data.Size = Marshal.SizeOf(data);

                if (!EnumDeviceInterfaces(lpDeviceInfoSet, nDeviceInfoData, gClass, index, ref data))
                {
                    var lastError = Marshal.GetLastWin32Error();
                    if (lastError == ERROR_NO_MORE_ITEMS)
                    {
                        yield break;
                    }

                    throw new Win32Exception(lastError);
                }

                yield return data;
                index++;
            }
        }

        /* The SetupDiGetDeviceInterfaceDetail function returns details about 
         * a device interface.*/
        [SuppressUnmanagedCodeSecurity]
        [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceInterfaceDetail")]
        public static extern bool GetDeviceInterfaceDetail(
            DeviceInfoListSafeHandle lpDeviceInfoSet, ref DeviceInterfaceData oInterfaceData,
            ref DeviceInterfaceDetailData oDetailData,
            uint nDeviceInterfaceDetailDataSize, ref uint nRequiredSize,
            IntPtr lpDeviceInfoData);

        /* destroys device list */
        [SuppressUnmanagedCodeSecurity]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiDestroyDeviceInfoList")]
        public static extern bool DestroyDeviceInfoList(IntPtr lpInfoSet);
    }
}