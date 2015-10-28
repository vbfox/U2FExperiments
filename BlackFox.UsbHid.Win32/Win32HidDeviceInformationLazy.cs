using System;
using System.ComponentModel;
using System.Diagnostics;
using JetBrains.Annotations;
using static PInvoke.Hid;
using static PInvoke.Kernel32;

namespace BlackFox.UsbHid.Win32
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal class Win32HidDeviceInformationLazy : Win32HidDeviceInformation
    {
        public override string Path { get;  }
        private readonly SafeObjectHandle handle;
        public override HidpCaps Capabilities => capabilities.Value;
        public override ushort ProductId => attributes.Value.ProductId;
        public override ushort VendorId => attributes.Value.VendorId;
        public override ushort Version => attributes.Value.VersionNumber;
        public override string Manufacturer => manufacturer.Value;
        public override string Product => product.Value;
        public override string SerialNumber => serialNumber.Value;

        private readonly Lazy<HiddAttributes> attributes;
        private readonly Lazy<HidpCaps> capabilities;
        private readonly Lazy<string> manufacturer;
        private readonly Lazy<string> serialNumber;
        private readonly Lazy<string> product;

        internal Win32HidDeviceInformationLazy(string path, [NotNull] SafeObjectHandle handle)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            if (handle.IsInvalid) throw new ArgumentException("Handle is invalid", nameof(handle));

            Path = path;
            this.handle = handle;

            manufacturer = GetValueOrNullLazy(HidD_GetManufacturerString);
            product = GetValueOrNullLazy(HidD_GetProductString);
            serialNumber = GetValueOrNullLazy(HidD_GetSerialNumberString);
            attributes = ExceptionWrappedLazy(() => HidD_GetAttributes(handle));
            capabilities = ExceptionWrappedLazy(GetCapabilities);
        }

        private delegate bool TryGetValue(SafeObjectHandle handle, out string value);

        private Lazy<string> GetValueOrNullLazy(TryGetValue tryGet)
        {
            return ExceptionWrappedLazy(
                () =>
                {
                    string value;
                    return tryGet(handle, out value) ? value : null;
                });
        }

        private static Lazy<T> ExceptionWrappedLazy<T>(Func<T> valueFactory)
        {
            return new Lazy<T>(() =>
            {
                try
                {
                    return valueFactory();
                }
                catch (Exception exception)
                {
                    throw ExceptionConversion.ConvertException(exception);
                }
            });
        }

        private HidpCaps GetCapabilities()
        {
            using (var preparsedData = HidD_GetPreparsedData(handle))
            {
                return HidP_GetCaps(preparsedData);
            }
        }

        internal override string DebuggerDisplay
        {
            get
            {
                var manufacturerValue = manufacturer.IsValueCreated ? manufacturer.Value : null;
                var productValue = product.IsValueCreated ? product.Value : null;
                ushort? productIdValue = null;
                ushort? vendorIdValue = null;
                if (attributes.IsValueCreated)
                {
                    productIdValue = attributes.Value.ProductId;
                    vendorIdValue = attributes.Value.VendorId;
                }

                return $"'{productValue}' (PID=0x{productIdValue:X2}) "
                    + $"by '{manufacturerValue}' (VID=0x{vendorIdValue:X2})";
            }
        }
    }
}