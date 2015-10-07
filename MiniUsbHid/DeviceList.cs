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

            using (var device = HidDevice.TryOpenForInfo(path))
            {
                if (device == null)
                {
                    return new DeviceInfo(path, false, null, null, null, 0, 0, 0, null);
                }

                return new DeviceInfo(
                        path,
                        true,
                        device.GetProduct(),
                        device.GetManufacturer(),
                        device.GetSerialNumber(),
                        device.VendorId,
                        device.ProductId,
                        device.Version,
                        device.GetCaps());
            }
        }
    }
}
