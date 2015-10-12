using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackFox.U2FHid.Core.RawPackets;
using BlackFox.UsbHid;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Core
{
    internal static class FidoU2FHidPaketReader
    {
        static FidoU2FHidMessage BuildMessage(InitializationPacket init, List<ContinuationPacket> continuations)
        {
            return new FidoU2FHidMessage(init.ChannelIdentifier, (U2FHidCommand)init.CommandIdentifier, init.Data);
        }

        public static async Task<FidoU2FHidMessage> ReadFidoU2FHidMessageAsync([NotNull] this IHidDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            var inputReport = await device.GetInputReportAsync();
            var init = InitializationPacket.ReadFrom(inputReport.Data);
            return BuildMessage(init, null);
        }
    }
}