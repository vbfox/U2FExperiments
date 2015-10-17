using System;

namespace BlackFox.U2F.Codec
{
    /// <summary>
    /// An APDU (Smart card application protocol data unit) query message.
    /// </summary>
    public struct ApduRequest
    {
        public byte CommandCode { get; }
        public byte Parameter1 { get; }
        public byte Parameter2 { get; }
        public ArraySegment<byte> Data { get; }

        public ApduRequest(byte commandCode, byte parameter1, byte parameter2, ArraySegment<byte> data)
        {
            CommandCode = commandCode;
            Parameter1 = parameter1;
            Parameter2 = parameter2;
            Data = data;
        }
    }
}