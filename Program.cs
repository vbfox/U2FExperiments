using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BlackFox.U2FHid;
using BlackFox.U2FHid.RawPackets;
using BlackFox.UsbHid.Portable;
using BlackFox.UsbHid.Win32;
using BlackFox.Win32.Kernel32;

namespace U2FExperiments
{
    class Program
    {
        const byte TYPE_INIT = 0x80;

        const int FIDO_USAGE_DATA_IN = 0x20; // Raw IN data report
        const int FIDO_USAGE_DATA_OUT = 0x21; // Raw OUT data report
        const uint U2FHID_BROADCAST_CID = 0xffffffff;

        private static void ShowDevices(ICollection<IHidDeviceInformation> deviceInfos)
        {
            foreach (var device in deviceInfos)
            {
                Console.WriteLine(" * {0}", device.Id);
                Console.WriteLine("   {0} {1} (VID=0x{2:X4}, PID=0x{3:X4}, SN={4})", device.Manufacturer, device.Product,
                    device.VendorId, device.ProductId, device.SerialNumber);
                if (device.IsFidoU2F())
                {
                    Console.WriteLine("   FIDO Device !");
                }
            }
        }

        static void Main(string[] args)
        {
            var factory = (IHidDeviceFactory) Win32HidDeviceFactory.Instance;
            var devices = factory.FindAllAsync().Result;
            var fidoInfo = devices.Where(FidoU2FIdentification.IsFidoU2F).FirstOrDefault();

            Console.WriteLine("Devices found:");
            ShowDevices(devices);
            Console.WriteLine();
            
            if (fidoInfo == null)
            {
                Console.WriteLine("Can't find FIDO device :-(");
                Console.ReadLine();
                return;
            }

            Console.WriteLine(fidoInfo.Id);
            Console.WriteLine(fidoInfo.Manufacturer);
            Console.WriteLine(fidoInfo.Product);
            Console.WriteLine(fidoInfo.SerialNumber);
            Console.WriteLine("VID = 0x{0:X4}", fidoInfo.VendorId);
            Console.WriteLine("PID = 0x{0:X4}", fidoInfo.ProductId);

            using (var device = (Win32HidDevice)fidoInfo.OpenDeviceAsync().Result)
            {
                device.SetNumInputBuffers(64);
                var caps = device.Information.Capabilities;
                Console.WriteLine(caps.NumberFeatureButtonCaps);

                Test(device);
                Console.ReadLine();
            }
        }

        static unsafe void Test(Win32HidDevice device)
        {
            var init = new U2FInitializationPacket();
            init.CommandIdentifier = (byte)U2FHidCommand.Init;
            init.ChannelIdentifier = U2FHID_BROADCAST_CID;
            init.PayloadLength = 8;
            var caps = device.Information.Capabilities;

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

        static unsafe void Wink(Win32HidDevice device, byte b1, byte b2, byte b3, byte b4)
        {
            var msg = new FidoU2FHidMessage(
                (uint)(unchecked (b1 << 24 | b2 << 16 | b3 << 8 | b4)),
                U2FHidCommand.Wink,
                EmptyArraySegment.Of<byte>());
            device.WriteFidoU2FHidMessageAsync(msg).Wait();

            var caps = device.Information.Capabilities;

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

    internal static class EmptyArraySegment
    {
        private static class Holder<T>
        {
            public static readonly ArraySegment<T> Empty = new ArraySegment<T>(new T[0]);
        }

        public static ArraySegment<T> Of<T>()
        {
            return Holder<T>.Empty;
        }
    }
}
