using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32.SafeHandles;
using U2FExperiments.Win32;
using U2FExperiments.Win32.Hid;
using U2FExperiments.Win32.Kernel32;
using U2FExperiments.Win32.SetupApi;

namespace U2FExperiments.MiniUsbHid
{
    public static class DeviceList
    {
        public static ICollection<DeviceInfo> Get()
        {
            using (var infoList = SetupApiDll.GetClassDevs(HidDll.HidGuid, null, IntPtr.Zero,
                GetClassDevsFlags.DeviceInterface | GetClassDevsFlags.Present))
            {
                return SetupApiDll.EnumDeviceInterfaces(infoList, 0, HidDll.HidGuid)
                    .Select(interfaceData => InfoFromData(infoList, interfaceData))
                    .ToList();
            }
        }

        static DeviceInfo InfoFromData(SafeDeviceInfoListHandle infoList, DeviceInterfaceData interfaceData)
        {
            var path = SetupApiDll.GetDeviceInterfaceDetail(infoList, interfaceData, IntPtr.Zero);

            using (var handle = OpenRead(path))
            {
                if (handle.IsInvalid)
                {
                    return new DeviceInfo(path, false, null, null, -1, -1, -1);
                }

                var device = new HidDevice(handle, false);

                var attributes = device.GetAttributes();

                return new DeviceInfo(
                        path,
                        true,
                        device.GetProduct(),
                        device.GetManufacturer(),
                        attributes.VendorId,
                        attributes.ProductId,
                        attributes.VersionNumber);
            }
        }

        private static SafeFileHandle OpenRead(string path)
        {
            return Kernel32Dll.NativeMethods.CreateFile(path,
                Native.GENERIC_READ,
                Native.FILE_SHARE_READ,
                IntPtr.Zero, Native.OPEN_EXISTING, 0,
                IntPtr.Zero);
        }

    }
}
