using System.Threading.Tasks;
using JetBrains.Annotations;

namespace BlackFox.UsbHid
{
    public interface IHidDeviceInformation
    {
        string Id { get; }

        ushort ProductId { get; }

        ushort VendorId { get; }

        ushort Version { get; }

        ushort UsageId { get; }

        ushort UsagePage { get; }

        [CanBeNull]
        string Manufacturer { get; }

        [CanBeNull]
        string Product { get; }

        [CanBeNull]
        string SerialNumber { get; }

        [NotNull]
        [ItemNotNull]
        Task<IHidDevice> OpenDeviceAsync(HidDeviceAccessMode accessMode = HidDeviceAccessMode.ReadWrite);
    }
}