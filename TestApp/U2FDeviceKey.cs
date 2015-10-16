using System;
using System.IO;
using BlackFox.Binary;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.messages;
using BlackFox.U2FHid;
using JetBrains.Annotations;
using Org.BouncyCastle.Crypto.Generators;

namespace U2FExperiments
{
    internal class FramingResponse
    {
        public ushort StatusWord { get; }

        public ArraySegment<byte> ResponseData { get; }

        public FramingResponse(ushort statusWord, ArraySegment<byte> responseData)
        {
            StatusWord = statusWord;
            ResponseData = responseData;
        }
    }

    internal static class FramingCodec
    {
        private static readonly int maxFramedDataSize = (int)Math.Pow(2, 24);

        private static void Write3ByteIntBigEndian(Stream stream, uint value)
        {
            var buffer = new byte[3];
            buffer[0] = (byte)(value >> 16);
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)value;
            stream.Write(buffer, 0, 3);
        }

        public static ArraySegment<byte> FrameRequest(byte commandCode, byte parameter1, byte parameter2, ArraySegment<byte> data)
        {
            if (data.Count > maxFramedDataSize)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "The data length is too big");

            }
            using (var stream = new MemoryStream())
            {
                stream.WriteByte(0);
                stream.WriteByte(commandCode);
                stream.WriteByte(parameter1);
                stream.WriteByte(parameter2);
                Write3ByteIntBigEndian(stream, (uint)data.Count);
                stream.Write(data.Array, data.Offset, data.Count);

                return stream.ToArray().Segment();
            }
        }

        public static FramingResponse ParseResponse(ArraySegment<byte> data)
        {
            if (data.Count < 2)
            {
                throw new Exception("Framed response is too small");
            }

            var lastByte = data.Offset + data.Count - 1;
            var statusWord = (ushort)(data.Array[lastByte-1] << 8 | data.Array[lastByte]);
            return new FramingResponse(statusWord, data.Segment(0, data.Count - 2));
        }
    }

    class U2FDeviceKey : IU2FKey
    {
        private readonly U2FDevice device;

        public U2FDeviceKey([NotNull] U2FDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            this.device = device;
        }

        public RegisterResponse Register(RegisterRequest registerRequest)
        {
            var requestBytes = RawMessageCodec.EncodeRegisterRequest(registerRequest);
            var framedRequestBytes = FramingCodec.FrameRequest(0x01, 0x00, 0x00, requestBytes.Segment());
            var response = device.SendU2FMessage(framedRequestBytes).Result;

            var parsedResponse = FramingCodec.ParseResponse(response);
            if (parsedResponse.StatusWord != 0x9000)
            {
                throw new Exception($"Bad response: {parsedResponse.StatusWord:X4}");
            }
            return RawMessageCodec.DecodeRegisterResponse(parsedResponse.ResponseData.ToNewArray());
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest authenticateRequest)
        {
            var requestBytes = RawMessageCodec.EncodeAuthenticateRequest(authenticateRequest);
            var response = device.SendU2FMessage(requestBytes.Segment()).Result;
            return RawMessageCodec.DecodeAuthenticateResponse(response.ToNewArray());
        }


    }
}
