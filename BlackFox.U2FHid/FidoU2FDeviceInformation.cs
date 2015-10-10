using System;
using System.Threading.Tasks;
using BlackFox.UsbHid;
using JetBrains.Annotations;

namespace BlackFox.U2FHid
{
    class FidoU2FDeviceInformation
    {
        [NotNull]
        readonly IHidDeviceInformation info;

        ushort ProductId { get; }

        ushort VendorId { get; }

        ushort Version { get; }

        [CanBeNull]
        string Manufacturer { get; }

        [CanBeNull]
        string Product { get; }

        [CanBeNull]
        string SerialNumber { get; }

        [NotNull]
        [ItemNotNull]
        Task<U2FDevice> OpenDeviceAsync()
        {
            return info
                .OpenDeviceAsync()
                .ContinueWith(
                    task => U2FDevice.Open(task.Result),
                    TaskContinuationOptions.OnlyOnRanToCompletion)
                .Unwrap();
        }

        public FidoU2FDeviceInformation([NotNull] IHidDeviceInformation info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            this.info = info;
        }
    }
}
