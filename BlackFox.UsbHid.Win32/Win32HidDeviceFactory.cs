using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.Win32.Kernel32;
using Common.Logging;
using JetBrains.Annotations;
using PInvoke;
using static PInvoke.Kernel32;
using static PInvoke.SetupApi;

namespace BlackFox.UsbHid.Win32
{
    public class Win32HidDeviceFactory : IHidDeviceFactory
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Win32HidDevice));

        private static readonly Lazy<Win32HidDeviceFactory> instance = new Lazy<Win32HidDeviceFactory>(() => new Win32HidDeviceFactory());
        private static readonly Guid hidGuid = Hid.HidD_GetHidGuid();
        public static Win32HidDeviceFactory Instance => instance.Value;

        private static Win32HidDevice FromId([NotNull] string deviceId, HidDeviceAccessMode accessMode,
            [CanBeNull] Win32HidDeviceInformation knownInformation)
        {
            if (deviceId == null) throw new ArgumentNullException(nameof(deviceId));
            switch (accessMode)
            {
                case HidDeviceAccessMode.Read:
                    return Win32HidDevice.FromPath(deviceId, FileAccess.GenericRead, knownInformation);
                case HidDeviceAccessMode.Write:
                    return Win32HidDevice.FromPath(deviceId, FileAccess.GenericWrite, knownInformation);
                case HidDeviceAccessMode.ReadWrite:
                    return Win32HidDevice.FromPath(deviceId,
                        FileAccess.GenericRead | FileAccess.GenericWrite, knownInformation);
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
            using (var infoList = SetupDiGetClassDevs(hidGuid, null, IntPtr.Zero,
                GetClassDevsFlags.DIGCF_DEVICEINTERFACE | GetClassDevsFlags.DIGCF_PRESENT))
            {
                return SetupDiEnumDeviceInterfaces(infoList, null, hidGuid)
                    .Select(interfaceData => InfoFromData(infoList, interfaceData))
                    .Where(i => i != null)
                    .ToList();
            }
        }

        private static IHidDeviceInformation InfoFromData(SafeDeviceInfoSetHandle infoList, DeviceInterfaceData interfaceData)
        {
            var path = SetupDiGetDeviceInterfaceDetail(infoList, interfaceData, IntPtr.Zero);

            using (var device = Win32HidDevice.TryFromPath(path, 0))
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
