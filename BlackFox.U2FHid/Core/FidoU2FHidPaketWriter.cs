using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2FHid.Core.RawPackets;
using BlackFox.UsbHid;
using Common.Logging;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Core
{
    public static class FidoU2FHidPaketWriter
    {
        static readonly ILog log = LogManager.GetLogger(typeof(FidoU2FHidPaketWriter));

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

        public static async Task WriteFidoU2FHidMessageAsync([NotNull] this IHidDevice device,
            FidoU2FHidMessage message, CancellationToken cancellationToken)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            log.Debug($"Sending U2FHid message {message.Command} on channel 0x{message.Channel:X8} with {message.Data.Count} bytes of data");
                
            var report = device.CreateOutputReport();
            var packets = MakeOutputPackets(report.Data.Count, message);
            packets.Item1.WriteTo(report.Data);
            
            await device.SendOutputReportAsync(report, cancellationToken);

            foreach (var continuation in packets.Item2)
            {
                await device.SendOutputReportAsync(ToOutputReport(device, continuation), cancellationToken);
            }
        }

        static IHidOutputReport ToOutputReport(IHidDevice device, ContinuationPacket continuation)
        {
            var report = device.CreateOutputReport();
            continuation.WriteTo(report.Data);
            return report;
        }
    }
}