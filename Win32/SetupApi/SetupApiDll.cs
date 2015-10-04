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
        public static SafeDeviceInfoListHandle GetClassDevs(Guid? gClass,
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
            SafeDeviceInfoListHandle lpDeviceInfoSet,
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
            SafeDeviceInfoListHandle lpDeviceInfoSet,
            uint nDeviceInfoData,
            Guid gClass)
        {
            uint index = 0;
            while (true)
            {
                var data = DeviceInterfaceData.Create();

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

        static readonly Lazy<int> deviceInterfaceDetailDataSize = new Lazy<int>(() =>
        {
            // The structure size take into account an Int32 and a character
            switch (IntPtr.Size)
            {
                case 4: // 32-bits
                    // The character can be 1 or 2 octets depending on ANSI / Unicode
                    return 4 + Marshal.SystemDefaultCharSize;
                case 8: // 64-bits
                    // Due to alignment, the size is always the same
                    return 8;
                default:
                    throw new NotSupportedException("Non 32 or 64-bits windows aren't supported");
            }
        });

        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        /* The SetupDiGetDeviceInterfaceDetail function returns details about 
         * a device interface.*/
        public static string GetDeviceInterfaceDetail(
            SafeDeviceInfoListHandle lpDeviceInfoSet, DeviceInterfaceData oInterfaceData,
            IntPtr lpDeviceInfoData)
        {
            using (var requiredSize = new NullableStructPtr<uint>(0))
            {
                NativeMethods.GetDeviceInterfaceDetail(lpDeviceInfoSet, ref oInterfaceData, IntPtr.Zero,
                    0, requiredSize.Handle, IntPtr.Zero);

                var lastError = Marshal.GetLastWin32Error();

                if (lastError != ERROR_INSUFFICIENT_BUFFER)
                {
                    throw new Win32Exception(lastError);
                }

                var buffer = Marshal.AllocHGlobal((int)requiredSize.Value);
                
                try
                {
                    Marshal.WriteInt32(buffer, deviceInterfaceDetailDataSize.Value);
                    var success = NativeMethods.GetDeviceInterfaceDetail(lpDeviceInfoSet, ref oInterfaceData, buffer,
                        requiredSize.Value, IntPtr.Zero, IntPtr.Zero);
                    if (!success)
                    {
                        throw new Win32Exception();
                    }

                    var strPtr = new IntPtr(buffer.ToInt64() + 4);
                    return Marshal.PtrToStringAuto(strPtr);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }

        /* destroys device list */
        [SuppressUnmanagedCodeSecurity]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("setupapi.dll", SetLastError = true, EntryPoint = "SetupDiDestroyDeviceInfoList")]
        public static extern bool DestroyDeviceInfoList(IntPtr lpInfoSet);
    }
}