using System;

namespace BlackFox.UsbHid.Portable
{
    public class HidInputReport
    {
        readonly ArraySegment<byte> buffer;

        public byte Id => buffer.Array[buffer.Offset];

        public ArraySegment<byte> Data { get; }

        public ArraySegment<byte> GetInputBuffer()
        {
            return buffer;
        }

        public HidInputReport(ArraySegment<byte> buffer)
        {
            this.buffer = buffer;
            Data = new ArraySegment<byte>(buffer.Array, buffer.Offset+1, buffer.Count-1);
        }
    }
}
