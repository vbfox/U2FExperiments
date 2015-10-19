using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace BlackFox.UsbHid
{
    public interface IHidDeviceFactory
    {
        [ItemNotNull]
        Task<IHidDevice> FromIdAsync([NotNull] string deviceId, HidDeviceAccessMode accessMode,
            CancellationToken cancellationToken = default(CancellationToken));

        [ItemNotNull]
        Task<ICollection<IHidDeviceInformation>> FindAllAsync(
            CancellationToken cancellationToken = default(CancellationToken));
    }
}