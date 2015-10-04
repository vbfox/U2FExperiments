using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using U2FExperiments.Win32;
using U2FExperiments.Win32.Hid;
using U2FExperiments.Win32.Kernel32;
using U2FExperiments.Win32.SetupApi;

namespace U2FExperiments
{
    public class HIDInfo
    {
        /* device path */
        public string Path { get; private set; }
        /* vendor ID */
        public short Vid { get; private set; }
        /* product id */
        public short Pid { get; private set; }
        /* usb product string */
        public string Product { get; private set; }
        /* usb manufacturer string */
        public string Manufacturer { get; private set; }
        /* usb serial number string */
        public string SerialNumber { get; private set; }

        /* constructor */
        public HIDInfo(string product, string serial, string manufacturer,
            string path, short vid, short pid)
        {
            /* copy information */
            Product = product;
            SerialNumber = serial;
            Manufacturer = manufacturer;
            Path = path;
            Vid = vid;
            Pid = pid;
        }
    }

    public class HIDBrowse
    {
        /* browse all HID class devices */
        public static List<HIDInfo> Browse()
        {
            /* list of device information */
            List<HIDInfo> info = new List<HIDInfo>();

            /* get list of present hid devices */
            using (var hInfoSet = SetupApiDll.GetClassDevs(HidDll.HidGuid, null, IntPtr.Zero,
                GetClassDevsFlags.DeviceInterface | GetClassDevsFlags.Present))
            {
                foreach (var iface in SetupApiDll.EnumDeviceInterfaces(hInfoSet, 0, HidDll.HidGuid))
                {
                    /* vid and pid */
                    short vid, pid;

                    /* get device path */
                    var path = GetPath(hInfoSet, iface);

                    /* open device */
                    using (var handle = Open(path))
                    {
                        if (!handle.IsInvalid)
                        {
                            var attributes = HidDll.GetAttributes(handle);

                            /* build up a new element */
                            var i = new HIDInfo(
                                HidDll.GetProductString(handle),
                                "",
                                HidDll.GetManufacturerString(handle),
                                path, attributes.VendorID, attributes.ProductID);
                            /* add to list */
                            info.Add(i);
                        }
                    }
                }
            }

            /* return list */
            return info;
        }

        /* open device */
        private static SafeFileHandle Open(string path)
        {
            /* opens hid device file */
            return Kernel32Dll.NativeMethods.CreateFile(path,
                Native.GENERIC_READ | Native.GENERIC_WRITE,
                Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE,
                IntPtr.Zero, Native.OPEN_EXISTING, Native.FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);
        }

        /* get device path */
        private static string GetPath(DeviceInfoListSafeHandle hInfoSet,
            DeviceInterfaceData iface)
        {
            return SetupApiDll.GetDeviceInterfaceDetail(hInfoSet, iface, IntPtr.Zero);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var browsed = HIDBrowse.Browse();
            foreach (var b in browsed)
            {
                Console.WriteLine("--------------------------");
                Console.WriteLine(b.Path);
                Console.WriteLine(b.SerialNumber);
                Console.WriteLine(b.Manufacturer);
                Console.WriteLine(b.Product);
                Console.WriteLine("VID = 0x{0:X4}", b.Vid);
                Console.WriteLine("PID = 0x{0:X4}", b.Pid);
            }
            Console.ReadLine();
        }
    }
}
