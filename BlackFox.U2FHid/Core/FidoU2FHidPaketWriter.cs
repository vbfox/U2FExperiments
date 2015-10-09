using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackFox.U2FHid.Core.RawPackets;
using BlackFox.UsbHid.Portable;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Core
{
    public static class FidoU2FHidPaketWriter
    {
        static Tuple<InitializationPacket, List<ContinuationPacket>> MakeOutputPackets(
            int paketLength, FidoU2FHidMessage message)
        {
            var availableInInit = paketLength - InitializationPacket.NoDataSize;
            var availableInContinuation = paketLength - ContinuationPacket.NoDataSize;
            var data = message.Data;

            var init = new InitializationPacket
            {
                ChannelIdentifier = message.Channel,
                CommandIdentifier = (byte)message.Command,
                PayloadLength = (ushort)data.Count,
                Data = new ArraySegment<byte>(data.Array, data.Offset,
                    Math.Min(data.Count, availableInInit))
            };

            var sizeHandled = init.Data.Count;
            var continuations = new List<ContinuationPacket>();
            byte sequence = 0;
            while (sizeHandled < data.Count)
            {
                var continuation = new ContinuationPacket
                {
                    ChannelIdentifier = message.Channel,
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

        public static Task WriteFidoU2FHidMessageAsync([NotNull] this IHidDevice device, [NotNull] FidoU2FHidMessage message)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var report = device.CreateOutputReport();
            var pakets = MakeOutputPackets(report.Data.Count, message);
            pakets.Item1.WriteTo(report.Data);
            
            Task task = device.SendOutputReportAsync(report);

            foreach (var continuation in pakets.Item2)
            {
                task = task
                    .ContinueWith(previous => device.SendOutputReportAsync(ToOutputReport(device, continuation)))
                    .Unwrap();
            }
            return task;
        }

        static HidOutputReport ToOutputReport(IHidDevice device, ContinuationPacket continuation)
        {
            var report = device.CreateOutputReport();
            continuation.WriteTo(report.Data);
            return report;
        }
    }
}