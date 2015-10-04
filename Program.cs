using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using U2FExperiments.MiniUsbHid;
using U2FExperiments.Win32;
using U2FExperiments.Win32.Hid;
using U2FExperiments.Win32.Kernel32;

namespace U2FExperiments
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct U2FInitializationPaket
    {
        public int ChannelIdentifier;
        public byte CommandIdentifier;
        public byte PayloadLengthHi;
        public byte PayloadLengthLo;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct U2FContinuationPaket
    {
        public int ChannelIdentifier;
        public byte PaketSequence;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var yubiInfo = DeviceList.Get().Single(i => i.VendorId == 0x1050);

            Console.WriteLine(yubiInfo.Path);
            Console.WriteLine(yubiInfo.Manufacturer);
            Console.WriteLine(yubiInfo.Product);
            Console.WriteLine("VID = 0x{0:X4}", yubiInfo.VendorId);
            Console.WriteLine("PID = 0x{0:X4}", yubiInfo.ProductId);

            using (var device = yubiInfo.OpenDevice())
            {
                device.SetNumInputBuffers(64);
                var caps = device.GetCaps();
                Console.WriteLine(caps.NumberFeatureButtonCaps);
            }

            Console.ReadLine();
        }

        private static SafeFileHandle Open(string path)
        {
            return Kernel32Dll.CreateFile(path,
                Native.GENERIC_READ | Native.GENERIC_WRITE,
                Native.FILE_SHARE_READ | Native.FILE_SHARE_WRITE,
                IntPtr.Zero, Native.OPEN_EXISTING, Native.FILE_FLAG_OVERLAPPED,
                IntPtr.Zero);
        }
    }
}
