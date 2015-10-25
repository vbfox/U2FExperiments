using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.Win32.Hid;
using BlackFox.Win32.Kernel32;
using BlackFox.Win32.SetupApi;
using Common.Logging;
using JetBrains.Annotations;

namespace BlackFox.UsbHid.Win32
{
    public class Win32HidDeviceFactory : IHidDeviceFactory
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Win32HidDevice));

        private static readonly Lazy<Win32HidDeviceFactory> instance = new Lazy<Win32HidDeviceFactory>(() => new Win32HidDeviceFactory());
        public static Win32HidDeviceFactory Instance => instance.Value;

        private static Win32HidDevice FromId([NotNull] string deviceId, HidDeviceAccessMode accessMode,
            [CanBeNull] Win32HidDeviceInformation knownInformation)
        {
            if (deviceId == null) throw new ArgumentNullException(nameof(deviceId));
            switch (accessMode)
            {
                case HidDeviceAccessMode.Read:
                    return Win32HidDevice.FromPath(deviceId, Kernel32FileAccess.GenericRead, knownInformation);
                case HidDeviceAccessMode.Write:
                    return Win32HidDevice.FromPath(deviceId, Kernel32FileAccess.GenericWrite, knownInformation);
                case HidDeviceAccessMode.ReadWrite:
                    return Win32HidDevice.FromPath(deviceId,
                        Kernel32FileAccess.GenericRead | Kernel32FileAccess.GenericWrite, knownInformation);
                default:
                    throw new ArgumentException("Access mode not supported: " + accessMode, nameof(accessMode));
            }
        }

        internal Task<IHidDevice> FromIdAsyncInferface(string deviceId, HidDeviceAccessMode accessMode,
            [CanBeNull] Win32HidDeviceInformation knownInformation, CancellationToken cancellationToken)
        {
            return TaskEx.Run(() => (IHidDevice)FromId(deviceId, accessMode, knownInformation), cancellationToken);
        }

        Task<IHidDevice> IHidDeviceFactory.FromIdAsync(string deviceId, HidDeviceAccessMode accessMode, CancellationToken cancellationToken)
        {
            return FromIdAsyncInferface(deviceId, accessMode, null, cancellationToken);
        }

        internal Task<Win32HidDevice> FromIdAsync(string deviceId, HidDeviceAccessMode accessMode,
            [CanBeNull] Win32HidDeviceInformation knownInformation, CancellationToken cancellationToken)
        {
            return TaskEx.Run(() => FromId(deviceId, accessMode, knownInformation), cancellationToken);
        }

        public Task<Win32HidDevice> FromIdAsync(string deviceId, HidDeviceAccessMode accessMode,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return FromIdAsync(deviceId, accessMode, null, cancellationToken);
        }

        Task<ICollection<IHidDeviceInformation>> IHidDeviceFactory.FindAllAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => FindAll(), cancellationToken);
        }

        public Task<ICollection<Win32HidDeviceInformation>> FindAllAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(
                () => (ICollection<Win32HidDeviceInformation>)FindAll().OfType<Win32HidDeviceInformation>().ToList(),
                cancellationToken);
        }

        private static ICollection<IHidDeviceInformation> FindAll()
        {
            using (var infoList = SetupApiDll.GetClassDevs(HidDll.HidGuid, null, IntPtr.Zero,
                GetClassDevsFlags.DeviceInterface | GetClassDevsFlags.Present))
            {
                return SetupApiDll.EnumDeviceInterfaces(infoList, 0, HidDll.HidGuid)
                    .Select(interfaceData => InfoFromData(infoList, interfaceData))
                    .Where(i => i != null)
                    .ToList();
            }
        }

        private static IHidDeviceInformation InfoFromData(SafeDeviceInfoListHandle infoList, DeviceInterfaceData interfaceData)
        {
            var path = SetupApiDll.GetDeviceInterfaceDetail(infoList, interfaceData, IntPtr.Zero);

            using (var device = Win32HidDevice.TryFromPath(path, Kernel32FileAccess.None))
            {
                if (device == null)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.Debug($"Unable to open device {path}");
                    }
                    return null;
                }

                var information = new Win32HidDeviceInformationStored(device.Information);
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Found device '{information.Product}' (PID=0x{information.ProductId:X2}) "
                        + $"by '{information.Manufacturer}' (VID=0x{information.VendorId:X2})");
                }
                return information;
            }
        }
    }
}
