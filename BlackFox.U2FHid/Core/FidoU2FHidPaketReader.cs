using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BlackFox.U2FHid.Core.RawPackets;
using BlackFox.UsbHid.Portable;
using JetBrains.Annotations;

namespace BlackFox.U2FHid.Core
{
    internal static class FidoU2FHidPaketReader
    {
        static FidoU2FHidMessage BuildMessage(InitializationPacket init, List<ContinuationPacket> continuations)
        {
            return new FidoU2FHidMessage(init.ChannelIdentifier, (U2FHidCommand)init.CommandIdentifier, init.Data);
        }

        public static Task<FidoU2FHidMessage> ReadFidoU2FHidMessageAsync([NotNull] this IHidDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            return device.GetInputReportAsync()
                .ContinueWith(task =>
                {
                    var init = InitializationPacket.ReadFrom(task.Result.Data);
                    return BuildMessage(init, null);
                });
        }
    }
}