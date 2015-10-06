using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using U2FExperiments.MiniUsbHid;

namespace U2FExperiments.FidoU2F
{
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
                CommandIdentifier = (byte)message.CommandIdentifier,
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
}