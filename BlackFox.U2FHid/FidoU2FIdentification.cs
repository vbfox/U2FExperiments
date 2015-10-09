using BlackFox.UsbHid.Portable;

namespace BlackFox.U2FHid
{
    public static class FidoU2FIdentification
    {
        /// <summary>
        /// FIDO alliance HID usage page
        /// </summary>
        public const ushort FidoUsagePage = 0xf1d0;

        /// <summary>
        /// U2FHID usage for top-level collection
        /// </summary>
        public const ushort FidoUsageU2Fhid = 0x01;

        public static bool IsFidoU2F(this IHidDeviceInformation information)
        {
            return information.UsagePage == FidoUsagePage && information.UsageId == FidoUsageU2Fhid;
        }
    }
}
