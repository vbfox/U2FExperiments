using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BlackFox.U2FHid.RawPackets;
using BlackFox.UsbHid;
using BlackFox.UsbHid.Portable;
using JetBrains.Annotations;

namespace BlackFox.U2FHid
{
    internal static class FidoU2FHidPaketReader
    {
        static U2FInitializationPacket ReadInitialization(ArraySegment<byte> data)
        {
            var result = new U2FInitializationPacket();

            using (var stream = new MemoryStream(data.Array, data.Offset, data.Count))
            {
                var reader = new BinaryReader(stream);
                result.ChannelIdentifier = reader.ReadUInt32();
                result.CommandIdentifier = reader.ReadByte();

                var payloadLengthHi = reader.ReadByte();
                var payloadLengthLo = reader.ReadByte();
                result.PayloadLength = (ushort) (payloadLengthHi << 8 | payloadLengthLo);
            }

            var remainingBytes = Math.Min(data.Count - U2FInitializationPacket.NoDataSize, result.PayloadLength);
            result.Data = new ArraySegment<byte>(data.Array, data.Offset + U2FInitializationPacket.NoDataSize,
                remainingBytes);

            return result;
        }

        static FidoU2FHidMessage BuildMessage(U2FInitializationPacket init, List<U2FContinuationPacket> continuations)
        {
            return new FidoU2FHidMessage(init.ChannelIdentifier, (U2FHidCommand)init.CommandIdentifier, init.Data);
        }

        public static Task<FidoU2FHidMessage> ReadFidoU2FHidMessageAsync([NotNull] this IHidDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            return device.GetInputReportAsync()
                .ContinueWith(task =>
                {
                    var init = ReadInitialization(task.Result.Data);
                    return BuildMessage(init, null);
                });
        }
    }
}