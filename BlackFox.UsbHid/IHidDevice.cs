using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace BlackFox.UsbHid
{
    public interface IHidDevice : IDisposable
    {
        [NotNull]
        IHidDeviceInformation Information { get; }

        [NotNull]
        IHidOutputReport CreateOutputReport(byte id = 0);

        [NotNull]
        Task<int> SendOutputReportAsync([NotNull] IHidOutputReport report,
            CancellationToken cancellationToken = default(CancellationToken));

        [NotNull]
        [ItemNotNull]
        Task<HidInputReport> GetInputReportAsync(
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
