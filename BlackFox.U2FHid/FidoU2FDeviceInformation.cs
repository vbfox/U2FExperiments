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
        async Task<U2FHidKey> OpenDeviceAsync()
        {
            var hidDevice = await info.OpenDeviceAsync();
            try
            {
                return await U2FHidKey.OpenAsync(hidDevice);
            }
            catch
            {
                hidDevice.Dispose();
                throw;
            }
        }

        public FidoU2FDeviceInformation([NotNull] IHidDeviceInformation info)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));

            this.info = info;
        }
    }
}
