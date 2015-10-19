using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.U2FHid.Core;
using BlackFox.UsbHid;
using JetBrains.Annotations;

namespace BlackFox.U2FHid
{
    public class U2FHidKeyFactory : IKeyFactory
    {
        private readonly IHidDeviceFactory deviceFactory;

        public U2FHidKeyFactory([NotNull] IHidDeviceFactory deviceFactory)
        {
            if (deviceFactory == null)
            {
                throw new ArgumentNullException(nameof(deviceFactory));
            }

            this.deviceFactory = deviceFactory;
        }

        async Task<ICollection<IKeyId>> IKeyFactory.FindAllAsync(CancellationToken cancellationToken)
        {
            var devices = await deviceFactory.FindAllAsync(cancellationToken);

            return devices
                .Where(d => d.IsFidoU2F())
                .Select(d => (IKeyId)new U2FHidKeyId(d))
                .ToList();
        }

        public async Task<ICollection<U2FHidKeyId>> FindAllAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var devices = await deviceFactory.FindAllAsync(cancellationToken);

            return devices
                .Where(d => d.IsFidoU2F())
                .Select(d => new U2FHidKeyId(d))
                .ToList();
        }
    }
}
