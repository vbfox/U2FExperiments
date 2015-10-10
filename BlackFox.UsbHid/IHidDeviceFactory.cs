using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace BlackFox.UsbHid
{
    public interface IHidDeviceFactory
    {
        [ItemNotNull]
        Task<IHidDevice> FromIdAsync([NotNull] string deviceId, HidDeviceAccessMode accessMode);

        [ItemNotNull]
        Task<ICollection<IHidDeviceInformation>> FindAllAsync();
    }
}
