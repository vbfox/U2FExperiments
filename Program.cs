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
                            /* get device manufacturer string */
                            var man = GetManufacturer(handle.DangerousGetHandle());
                            /* get product string */
                            var prod = GetProduct(handle.DangerousGetHandle());
                            /* get serial number */
                            var serial = GetSerialNumber(handle.DangerousGetHandle());
                            /* get vid and pid */
                            GetVidPid(handle.DangerousGetHandle(), out vid, out pid);

                            /* build up a new element */
                            var i = new HIDInfo(prod, serial, man, path, vid, pid);
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
            return Kernel32Dll.CreateFileExtern(path,
                Native.GENERIC_READ | Native.GENERIC_WRITE,
                Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE,
                IntPtr.Zero, Native.OPEN_EXISTING, Native.FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);
        }

        /* get device path */
        private static string GetPath(DeviceInfoListSafeHandle hInfoSet,
            DeviceInterfaceData iface)
        {
            /* detailed interface information */
            var detIface = new DeviceInterfaceDetailData();
            /* required size */
            uint reqSize = (uint)Marshal.SizeOf(detIface);

            /* set size. The cbSize member always contains the size of the 
             * fixed part of the data structure, not a size reflecting the 
             * variable-length string at the end. */
            /* now stay with me and look at that x64/x86 maddness! */
            detIface.Size = Marshal.SizeOf(typeof(IntPtr)) == 8 ? 8 : 5;

            /* get device path */
            bool status = SetupApiDll.GetDeviceInterfaceDetail(hInfoSet,
                ref iface, ref detIface, reqSize, ref reqSize, IntPtr.Zero);

            /* whops */
            if (!status)
            {
                /* fail! */
                throw new Win32Exception();
            }

            /* return device path */
            return detIface.DevicePath;
        }

        /* get device manufacturer string */
        private static string GetManufacturer(IntPtr handle)
        {
            /* buffer */
            var s = new StringBuilder(256);
            /* returned string */
            string rc = String.Empty;

            /* get string */
            if (HidDll.GetManufacturerString(handle, s, s.Capacity))
            {
                rc = s.ToString();
            }

            /* report string */
            return rc;
        }

        /* get device product string */
        private static string GetProduct(IntPtr handle)
        {
            /* buffer */
            var s = new StringBuilder(256);
            /* returned string */
            string rc = String.Empty;

            /* get string */
            if (HidDll.GetProductString(handle, s, s.Capacity))
            {
                rc = s.ToString();
            }

            /* report string */
            return rc;
        }

        /* get device product string */
        private static string GetSerialNumber(IntPtr handle)
        {
            /* buffer */
            var s = new StringBuilder(256);
            /* returned string */
            string rc = String.Empty;

            /* get string */
            if (HidDll.GetSerialNumberString(handle, s, s.Capacity))
            {
                rc = s.ToString();
            }

            /* report string */
            return rc;
        }

        /* get vid and pid */
        private static void GetVidPid(IntPtr handle, out short Vid, out short Pid)
        {
            /* attributes structure */
            var attr = new HiddAttributes();
            /* set size */
            attr.Size = Marshal.SizeOf(attr);

            /* get attributes */
            if (HidDll.GetAttributes(handle, ref attr) == false)
            {
                /* fail! */
                throw new Win32Exception();
            }

            /* update vid and pid */
            Vid = attr.VendorID; Pid = attr.ProductID;
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
                Console.WriteLine(b.Manufacturer);
                Console.WriteLine(b.Product);
                Console.WriteLine(b.Vid);
                Console.WriteLine(b.Pid);
            }
            Console.ReadLine();
        }
    }
}
