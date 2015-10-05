using System;
using System.Collections.Generic;
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
        const int TYPE_INIT = 0x80;
        const int U2FHID_INIT = TYPE_INIT | 0x06;
        const int U2FHID_WINK = TYPE_INIT | 0x08;

        const int FIDO_USAGE_PAGE = 0xf1d0; // FIDO alliance HID usage page
        const int FIDO_USAGE_U2FHID = 0x01; // U2FHID usage for top-level collection
        const int FIDO_USAGE_DATA_IN = 0x20; // Raw IN data report
        const int FIDO_USAGE_DATA_OUT = 0x21; // Raw OUT data report
        const int U2FHID_BROADCAST_CID = unchecked ((int)0xffffffff);

        static bool IsFidoU2fDevice(DeviceInfo deviceInfo)
        {
            if (!deviceInfo.CanBeOpened)
            {
                return false;
            }

            using (var device = deviceInfo.OpenDevice())
            {
                var caps = device.GetCaps();
                return caps.UsagePage == FIDO_USAGE_PAGE && caps.Usage == FIDO_USAGE_U2FHID;
            }
        }

        public static IEnumerable<DeviceInfo> GetFidoU2FDevices()
        {
            return DeviceList.Get().Where(IsFidoU2fDevice);
        }

        static void Main(string[] args)
        {
            var fidoInfo = GetFidoU2FDevices().First();

            Console.WriteLine(fidoInfo.Path);
            Console.WriteLine(fidoInfo.Manufacturer);
            Console.WriteLine(fidoInfo.Product);
            Console.WriteLine("VID = 0x{0:X4}", fidoInfo.VendorId);
            Console.WriteLine("PID = 0x{0:X4}", fidoInfo.ProductId);

            using (var device = fidoInfo.OpenDevice())
            {
                device.SetNumInputBuffers(64);
                var caps = device.GetCaps();
                Console.WriteLine(caps.NumberFeatureButtonCaps);

                Test(device);
                
            }

            Console.ReadLine();
        }

        static unsafe void Test(Device device)
        {
            var init = new U2FInitializationPaket();
            init.CommandIdentifier = U2FHID_INIT;
            init.ChannelIdentifier = U2FHID_BROADCAST_CID;
            init.PayloadLengthLo = 8;
            init.PayloadLengthHi = 0;
            var caps = device.GetCaps();

            var buffer = new byte[caps.InputReportByteLength];
            buffer[0] = 0x00;
            buffer[8] = 0xCA;
            buffer[9] = 0xFE;
            buffer[10] = 0xBA;
            buffer[11] = 0xBE;
            buffer[12] = 0xDE;
            buffer[13] = 0xAD;
            buffer[14] = 0xBA;
            buffer[15] = 0xBE;

            fixed (byte* pBuffer = buffer)
            {
                Marshal.StructureToPtr(init, new IntPtr(pBuffer + 1), false);

                var task = Kernel32Dll.WriteFileAsync(device.Handle, new IntPtr(pBuffer), buffer.Length);
                var writen = task.Result;
                Console.WriteLine("Writen {0} bytes", writen);
            }

            var bufferOut = new byte[caps.OutputReportByteLength];
            fixed (byte* pBuffer = bufferOut)
            {
                var intPtr = new IntPtr(pBuffer);
                var task = Kernel32Dll.ReadFileAsync(device.Handle, intPtr, bufferOut.Length);
                var read = task.Result;
                Console.WriteLine("Read {0} bytes", read);
            }

            int i = 0;
            foreach(var b in bufferOut)
            {
                Console.Write("{0:X2} ", b);
                i++;
                if (i % 16 == 0)
                {
                    Console.WriteLine();
                }
            }

            if (i % 16 != 0)
            {
                Console.WriteLine();
            }

            Wink(device, bufferOut[16], bufferOut[17], bufferOut[18], bufferOut[19]);
        }

        static unsafe void Wink(Device device, byte b1, byte b2, byte b3, byte b4)
        {
            var init = new U2FInitializationPaket();
            init.CommandIdentifier = U2FHID_WINK;
            init.ChannelIdentifier = U2FHID_BROADCAST_CID;
            init.PayloadLengthLo = 0;
            init.PayloadLengthHi = 0;
            var caps = device.GetCaps();

            var buffer = new byte[caps.InputReportByteLength];

            fixed (byte* pBuffer = buffer)
            {
                Marshal.StructureToPtr(init, new IntPtr(pBuffer + 1), false);

                buffer[1] = b1;
                buffer[2] = b2;
                buffer[3] = b3;
                buffer[4] = b4;

                var task = Kernel32Dll.WriteFileAsync(device.Handle, new IntPtr(pBuffer), buffer.Length);
                var writen = task.Result;
                Console.WriteLine("Writen {0} bytes", writen);
            }

            var bufferOut = new byte[caps.OutputReportByteLength];
            fixed (byte* pBuffer = bufferOut)
            {
                var intPtr = new IntPtr(pBuffer);
                var task = Kernel32Dll.ReadFileAsync(device.Handle, intPtr, bufferOut.Length);
                var read = task.Result;
                Console.WriteLine("Read {0} bytes", read);
            }

            int i = 0;
            foreach (var b in bufferOut)
            {
                Console.Write("{0:X2} ", b);
                i++;
                if (i % 16 == 0)
                {
                    Console.WriteLine();
                }
            }

            if (i % 16 != 0)
            {
                Console.WriteLine();
            }
        }
    }
}
