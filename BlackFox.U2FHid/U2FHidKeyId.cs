using System;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.UsbHid;
using JetBrains.Annotations;

namespace BlackFox.U2FHid
{
    public class U2FHidKeyId : IKeyId
    {
        public IHidDeviceInformation HidDeviceInformation { get; }
        public string Product => HidDeviceInformation.Product;
        public string Manufacturer => HidDeviceInformation.Manufacturer;

        public U2FHidKeyId([NotNull] IHidDeviceInformation hidDeviceInformation)
        {
            if (hidDeviceInformation == null)
            {
                throw new ArgumentNullException(nameof(hidDeviceInformation));
            }

            HidDeviceInformation = hidDeviceInformation;
        }

        async Task<IKey> IKeyId.OpenAsync(CancellationToken cancellationToken)
        {
            return await OpenAsync(cancellationToken);
        }

        public async Task<U2FHidKey> OpenAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var device = await HidDeviceInformation.OpenDeviceAsync();
            return new U2FHidKey(device, true);
        }

        public override string ToString()
        {
            return $"{Product} by {Manufacturer}";
        }
    }
}
