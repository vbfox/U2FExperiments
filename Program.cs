using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;
using U2FExperiments.MiniUsbHid;
using U2FExperiments.Win32;
using U2FExperiments.Win32.Hid;
using U2FExperiments.Win32.Kernel32;

namespace U2FExperiments
{
    public class FidoU2FHidMessage
    {
        public uint ChannelIdentifier { get; }
        public byte CommandIdentifier { get; }
        public ArraySegment<byte> Data { get; }

        public FidoU2FHidMessage(uint channelIdentifier, byte commandIdentifier, ArraySegment<byte> data)
        {
            ChannelIdentifier = channelIdentifier;
            CommandIdentifier = commandIdentifier;
            Data = data;
        }
    }

    public struct U2FInitializationPacket
    {
        public uint ChannelIdentifier;
        public byte CommandIdentifier;
        public ushort PayloadLength;
        public ArraySegment<byte> Data;

        public static readonly int NoDataSize = 4 + 1 + 2;
    }

    public struct U2FContinuationPacket
    {
        public uint ChannelIdentifier;
        public byte PaketSequence;
        public ArraySegment<byte> Data;

        public static readonly int NoDataSize = 4 + 1;
    }

    interface IWritableUsbDevice
    {
        int MaximumInputPaketSize { get; }
        Task Write(ArraySegment<byte> data);
    }

    static class FidoU2FHidPaketWriter
    {
        static void WriteToStream(Stream stream, U2FInitializationPacket packet)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(packet.ChannelIdentifier);
            writer.Write(packet.CommandIdentifier);
            writer.Write((packet.PayloadLength >> 8) & 0xFF);
            writer.Write((packet.PayloadLength >> 0) & 0xFF);
            stream.Write(packet.Data.Array, packet.Data.Offset, packet.Data.Count);
        }

        static HidOutputReport ToOutputReport(HidDevice device, U2FInitializationPacket packet)
        {
            var result = device.CreateOutputReport();
            using (var stream = new MemoryStream(result.Data.Array, result.Data.Offset, result.Data.Count))
            {
                WriteToStream(stream, packet);
            }
            return result;
        }

        static void WriteToStream(Stream stream, U2FContinuationPacket packet)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(packet.ChannelIdentifier);
            writer.Write(packet.PaketSequence);
            stream.Write(packet.Data.Array, packet.Data.Offset, packet.Data.Count);
        }

        static HidOutputReport ToOutputReport(HidDevice device, U2FContinuationPacket packet)
        {
            var result = device.CreateOutputReport();
            using (var stream = new MemoryStream(result.Data.Array, result.Data.Offset, result.Data.Count))
            {
                WriteToStream(stream, packet);
            }
            return result;
        }

        static Tuple<U2FInitializationPacket, List<U2FContinuationPacket>> MakeOutputPackets(
            int paketLength, FidoU2FHidMessage message)
        {
            var availableInInit = paketLength - U2FInitializationPacket.NoDataSize;
            var availableInContinuation = paketLength - U2FContinuationPacket.NoDataSize;
            var data = message.Data;

            var init = new U2FInitializationPacket
            {
                ChannelIdentifier = message.ChannelIdentifier,
                CommandIdentifier = message.CommandIdentifier,
                PayloadLength = (ushort)data.Count,
                Data = new ArraySegment<byte>(data.Array, data.Offset,
                    Math.Min(data.Count, availableInInit))
            };

            var sizeHandled = init.Data.Count;
            var continuations = new List<U2FContinuationPacket>();
            byte sequence = 0;
            while (sizeHandled < data.Count)
            {
                var continuation = new U2FContinuationPacket
                {
                    ChannelIdentifier = message.ChannelIdentifier,
                    PaketSequence = sequence,
                    Data = new ArraySegment<byte>(data.Array, data.Offset + sizeHandled,
                        Math.Min(data.Count - sizeHandled, availableInContinuation))
                };

                continuations.Add(continuation);

                sizeHandled += continuation.Data.Count;
                sequence += 1;
            }

            return Tuple.Create(init, continuations);
        }

        public static Task WriteFidoU2FHidMessageAsync(this HidDevice device, [NotNull] FidoU2FHidMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var paketLength = device.GetCaps().OutputReportByteLength - 1;
            var pakets = MakeOutputPackets(paketLength, message);

            Task task = device.SendOutputReportAsync(ToOutputReport(device, pakets.Item1));
            foreach (var continuation in pakets.Item2)
            {
                task = task
                    .ContinueWith(previous => device.SendOutputReportAsync(ToOutputReport(device, continuation)))
                    .Unwrap();
            }
            return task;
        }
    }

    class Program
    {
        const byte TYPE_INIT = 0x80;
        const byte U2FHID_INIT = TYPE_INIT | 0x06;
        const byte U2FHID_WINK = TYPE_INIT | 0x08;

        const ushort FIDO_USAGE_PAGE = 0xf1d0; // FIDO alliance HID usage page
        const ushort FIDO_USAGE_U2FHID = 0x01; // U2FHID usage for top-level collection
        const int FIDO_USAGE_DATA_IN = 0x20; // Raw IN data report
        const int FIDO_USAGE_DATA_OUT = 0x21; // Raw OUT data report
        const uint U2FHID_BROADCAST_CID = 0xffffffff;

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

        static unsafe void Test(HidDevice device)
        {
            var init = new U2FInitializationPacket();
            init.CommandIdentifier = U2FHID_INIT;
            init.ChannelIdentifier = U2FHID_BROADCAST_CID;
            init.PayloadLength = 8;
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

        static unsafe void Wink(HidDevice device, byte b1, byte b2, byte b3, byte b4)
        {
            var msg = new FidoU2FHidMessage((uint)(unchecked (b1 << 24 | b2 << 16 | b3 << 8 | b4)), U2FHID_WINK,
                new ArraySegment<byte>(new byte[0]));
            device.WriteFidoU2FHidMessageAsync(msg);

            var caps = device.GetCaps();
            /*
            var init = new U2FInitializationPacket();
            init.CommandIdentifier = U2FHID_WINK;
            init.ChannelIdentifier = U2FHID_BROADCAST_CID;
            init.PayloadLengthLo = 0;
            init.PayloadLengthHi = 0;
            

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
            }*/

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
