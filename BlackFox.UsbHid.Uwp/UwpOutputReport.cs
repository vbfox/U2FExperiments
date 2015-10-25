using System;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage.Streams;

namespace BlackFox.UsbHid.Uwp
{
    class UwpOutputReport : IHidOutputReport
    {
        readonly HidOutputReport report;
        public byte Id => (byte)report.Id;
        public ArraySegment<byte> Data { get; }

        public UwpOutputReport(HidOutputReport report)
        {
            this.report = report;
            Data = new ArraySegment<byte>(new byte[report.Data.Capacity-1]);
        }

        public HidOutputReport GetFilledReport()
        {
            var dataWriter = new DataWriter();
            dataWriter.WriteByte(Id);
            dataWriter.WriteBytes(Data.Array);
            report.Data = dataWriter.DetachBuffer();

            return report;
        }
    }
}