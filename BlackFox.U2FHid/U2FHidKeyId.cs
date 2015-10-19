using System;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.UsbHid;
using JetBrains.Annotations;

namespace BlackFox.U2FHid
{
    public class U2FHidKeyId : IKeyId, IEquatable<U2FHidKeyId>
    {
        [NotNull]
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
            return await U2FHidKey.OpenAsync(device);
        }

        public bool Equals(IKeyId other)
        {
            var u2FHidKeyId = other as U2FHidKeyId;
            return u2FHidKeyId != null && Equals(u2FHidKeyId);
        }

        public bool Equals(U2FHidKeyId other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Equals(HidDeviceInformation.Id, other.HidDeviceInformation.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((U2FHidKeyId) obj);
        }

        public override int GetHashCode()
        {
            return HidDeviceInformation.Id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Product} by {Manufacturer}";
        }
    }
}
