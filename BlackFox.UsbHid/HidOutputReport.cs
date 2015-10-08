using System;

namespace BlackFox.UsbHid.Portable
{
    public class HidOutputReport
    {
        readonly ArraySegment<byte> buffer;

        public byte Id => buffer.Array[buffer.Offset];

        public ArraySegment<byte> Data { get; }

        public ArraySegment<byte> GetOutputBuffer()
        {
            return buffer;
        }

        public HidOutputReport(byte id, ArraySegment<byte> buffer)
        {
            buffer.Array[buffer.Offset] = id;

            this.buffer = buffer;
            Data = new ArraySegment<byte>(buffer.Array, buffer.Offset + 1, buffer.Count - 1);
        }
    }
}
