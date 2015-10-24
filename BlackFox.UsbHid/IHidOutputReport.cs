using System;

namespace BlackFox.UsbHid
{
    public interface IHidOutputReport
    {
        byte Id { get; }
        ArraySegment<byte> Data { get; }
    }
}
