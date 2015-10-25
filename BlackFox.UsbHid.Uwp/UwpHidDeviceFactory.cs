using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage;
using Windows.UI.Core;

namespace BlackFox.UsbHid.Uwp
{
    public class UwpHidDeviceFactory : IHidDeviceFactory
    {
        readonly CoreDispatcher uiDispatcher;

        public UwpHidDeviceFactory(CoreDispatcher uiDispatcher)
        {
            this.uiDispatcher = uiDispatcher;
        }

        static FileAccessMode ConvertAccessMode(HidDeviceAccessMode mode)
        {
            switch (mode)
            {
                case HidDeviceAccessMode.Read:
                    return FileAccessMode.Read;

                case HidDeviceAccessMode.Write:
                    return FileAccessMode.ReadWrite;

                case HidDeviceAccessMode.ReadWrite:
                    return FileAccessMode.ReadWrite;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown mode");
            }
        }

        public async Task<IHidDevice> FromIdAsync(string deviceId, HidDeviceAccessMode accessMode,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var uwpAccessMode = ConvertAccessMode(accessMode);

            HidDevice device = null;
            await uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                device = HidDevice.FromIdAsync(deviceId, uwpAccessMode).AsTask(cancellationToken).Result;
            });

            if (device == null)
            {
                throw new InvalidOperationException("Can't create device");
            }

            return new UwpHidDevice(device);
        }

        const string EnabledUsbHidSelector =
            @"System.Devices.InterfaceClassGuid:=""{4D1E55B2-F16F-11CF-88CB-001111000030}"""
            + @" AND System.Devices.InterfaceEnabled:=System.StructuredQueryType.Boolean#True";

        static readonly string[] wantedProperties =
            {
                UwpDevicePropertyNames.VendorId,
                UwpDevicePropertyNames.ProductId,
                UwpDevicePropertyNames.UsagePage,
                UwpDevicePropertyNames.UsageId
            };

        public async Task<ICollection<IHidDeviceInformation>> FindAllAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var raw = await DeviceInformation.FindAllAsync(EnabledUsbHidSelector, wantedProperties);

            return raw.Select(di => (IHidDeviceInformation)new UwpHidDeviceInformation(this, di)).ToList();
        }
    }
}
