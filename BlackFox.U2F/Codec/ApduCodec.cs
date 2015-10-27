using System;
using System.IO;
using BlackFox.Binary;

namespace BlackFox.U2F.Codec
{
    /// <summary>
    /// Encoder/Decoder for APDU (Smart card application protocol data unit) messages.
    /// </summary>
    public static class ApduCodec
    {
        private static readonly int maxDataSize = (int)Math.Pow(2, 24);

        private static void Write3ByteIntBigEndian(Stream stream, uint value)
        {
            var buffer = new byte[3];
            buffer[0] = (byte)(value >> 16);
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)value;
            stream.Write(buffer, 0, 3);
        }

        public static ArraySegment<byte> EncodeRequest(ApduRequest request)
        {
            if (request.Data.Count > maxDataSize)
            {
                throw new ArgumentOutOfRangeException(nameof(request), "The data length is too big");
            }

            var buffer = new byte[7 + request.Data.Count];
            using (var stream = new MemoryStream(buffer))
            {
                stream.WriteByte(0);
                stream.WriteByte(request.CommandCode);
                stream.WriteByte(request.Parameter1);
                stream.WriteByte(request.Parameter2);
                Write3ByteIntBigEndian(stream, (uint)request.Data.Count);
                stream.Write(request.Data.Array, request.Data.Offset, request.Data.Count);
            }

            return buffer.Segment();
        }

        public static ApduResponse DecodeResponse(ArraySegment<byte> data)
        {
            if (data.Count < 2)
            {
                throw new ArgumentException("Framed response is too small");
            }

            var lastByte = data.Offset + data.Count - 1;
            var statusWord = (ushort)(data.Array[lastByte-1] << 8 | data.Array[lastByte]);
            return new ApduResponse((ApduResponseStatus)statusWord, data.Segment(0, data.Count - 2));
        }

        public static ArraySegment<byte> EncodeResponse(ApduResponse response)
        {
            var buffer = new byte[response.ResponseData.Count + 2];

            using (var stream = new EndianWriter(new MemoryStream(buffer), Endianness.BigEndian))
            {
                stream.Write(response.ResponseData.Array, response.ResponseData.Offset, response.ResponseData.Count);
                var status = (short)response.Status;
                stream.Write(status);
            }

            return buffer.Segment();
        }
    }
}