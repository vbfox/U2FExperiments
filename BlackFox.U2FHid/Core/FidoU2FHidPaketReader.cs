using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BlackFox.Binary;
using BlackFox.U2FHid.Core.RawPackets;
using BlackFox.UsbHid;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Core
{
    internal static class FidoU2FHidPaketReader
    {
        static FidoU2FHidMessage BuildMessage(InitializationPacket init, List<ContinuationPacket> continuations)
        {
            ArraySegment<byte> data;
            if (continuations.Count > 0)
            {
                // We must allocate memory to reconstruct the message
                var stream = new MemoryStream(init.PayloadLength);
                stream.Write(init.Data.Array, init.Data.Offset, init.Data.Count);
                foreach (var continuation in continuations)
                {
                    stream.Write(continuation.Data.Array, continuation.Data.Offset, continuation.Data.Count);
                }
                data = stream.ToArray().Segment();
            }
            else
            {
                // No need to reconstruct the message
                data = init.Data;
            }

            return new FidoU2FHidMessage(init.ChannelIdentifier, (U2FHidCommand)init.CommandIdentifier, data);
        }

        public static async Task<FidoU2FHidMessage> ReadFidoU2FHidMessageAsync([NotNull] this IHidDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            var initReport = await device.GetInputReportAsync();
            var init = InitializationPacket.ReadFrom(initReport.Data);

            var dataSizeReceived = init.Data.Count;
            var continuations = new List<ContinuationPacket>();
            byte index = 0;
            while (dataSizeReceived < init.PayloadLength)
            {
                var continuationReport = await device.GetInputReportAsync();
                var continuation = ContinuationPacket.ReadFrom(continuationReport.Data, init.PayloadLength - dataSizeReceived);
                if (continuation.PaketSequence != index)
                {
                    throw new InvalidSequenceNumberException(continuationReport.Data,
                        $"The sequence number isn't the expected one, expected {index} but read {continuation.PaketSequence}");
                }
                continuations.Add(continuation);
                dataSizeReceived += continuation.Data.Count;
                index += 1;
            }

            return BuildMessage(init, continuations);
        }
    }
}