using System;

namespace BlackFox.UsbHid
{
    public class HidInputReport
    {
        readonly ArraySegment<byte> buffer;

        public byte Id { get; }

        public ArraySegment<byte> Data { get; }

        public HidInputReport(ArraySegment<byte> buffer)
        {
            this.buffer = buffer;
            Id = buffer.Array[buffer.Offset];
            Data = new ArraySegment<byte>(buffer.Array, buffer.Offset+1, buffer.Count-1);
        }

        public HidInputReport(byte id, ArraySegment<byte> data)
        {
            Id = id;
            Data = data;
        }
    }
}
