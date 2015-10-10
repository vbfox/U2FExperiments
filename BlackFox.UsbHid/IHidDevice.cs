﻿using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace BlackFox.UsbHid
{
    public interface IHidDevice : IDisposable
    {
        [NotNull]
        IHidDeviceInformation Information { get; }

        [NotNull]
        HidOutputReport CreateOutputReport(byte id = 0);
        Task<int> SendOutputReportAsync([NotNull] HidOutputReport report);

        [NotNull]
        [ItemNotNull]
        Task<HidInputReport> GetInputReportAsync();
    }
}
