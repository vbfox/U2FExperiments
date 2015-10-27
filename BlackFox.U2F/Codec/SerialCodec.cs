using System;
using System.IO;
using BlackFox.Binary;
using BlackFox.U2F.Gnubby.Messages;

namespace BlackFox.U2F.Codec
{
    public class SerialCodec
    {
        public const byte Version = 0x02;

        public const byte CommandRegister = 0x01;

        public const byte CommandAuthenticate = 0x02;

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="U2FException"/>
        public static void SendRegisterRequest(Stream outputStream, KeyRegisterRequest keyRegisterRequest)
        {
            SendRequest(outputStream, CommandRegister, RawMessageCodec.
                EncodeKeyRegisterRequest(keyRegisterRequest));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="U2FException"/>
        public static void SendRegisterResponse(Stream outputStream, KeyRegisterResponse keyRegisterResponse)
        {
            SendResponse(outputStream, RawMessageCodec.EncodeKeyRegisterResponse(keyRegisterResponse));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="U2FException"/>
        public static void SendAuthenticateRequest(Stream outputStream, KeySignRequest keySignRequest)
        {
            SendRequest(outputStream, CommandAuthenticate,
                RawMessageCodec.EncodeKeySignRequest(keySignRequest));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="U2FException"/>
        public static void SendAuthenticateResponse(Stream outputStream, KeySignResponse
            keySignResponse)
        {
            SendResponse(outputStream, RawMessageCodec.EncodeKeySignResponse(keySignResponse));
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        static void SendRequest(Stream outputStream, byte command, byte[] encodedBytes)
        {
            if (encodedBytes.Length > 65535)
            {
                throw new U2FException("Message is too long to be transmitted over this protocol");
            }
            using (var dataOutputStream = new EndianWriter(outputStream, Endianness.BigEndian))
            {
                dataOutputStream.Write(Version);
                dataOutputStream.Write(command);
                dataOutputStream.Write((short)encodedBytes.Length);
                dataOutputStream.Write(encodedBytes);
            }
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        static void SendResponse(Stream outputStream, byte[] encodedBytes)
        {
            if (encodedBytes.Length > 65535)
            {
                throw new U2FException("Message is too long to be transmitted over this protocol");
            }
            using (var dataOutputStream = new EndianWriter(outputStream, Endianness.BigEndian))
            {
                dataOutputStream.Write((short)encodedBytes.Length);
                dataOutputStream.Write(encodedBytes);
            }
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        public static IU2FRequest ParseRequest(Stream inputStream)
        {
            using (var dataInputStream = new EndianReader(inputStream, Endianness.BigEndian))
            {
                var version = dataInputStream.ReadByte();
                if (version != Version)
                {
                    throw new U2FException($"Unsupported message version: {version}");
                }
                var command = dataInputStream.ReadByte();
                switch (command)
                {
                    case CommandRegister:
                    {
                        return RawMessageCodec.DecodeKeyRegisterRequest(ParseMessage(dataInputStream
                            ));
                    }

                    case CommandAuthenticate:
                    {
                        return RawMessageCodec.DecodeKeySignRequest(ParseMessage
                            (dataInputStream));
                    }

                    default:
                    {
                        throw new U2FException($"Unsupported command: {command}");
                    }
                }
            }
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        public static KeyRegisterResponse ParseRegisterResponse(Stream inputStream)
        {
            using (var dataInputStream = new EndianReader(inputStream, Endianness.BigEndian))
            {
                return RawMessageCodec.DecodeKeyRegisterResponse(ParseMessage(dataInputStream).Segment());
            }
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        public static KeySignResponse ParseAuthenticateResponse(Stream inputStream)
        {
            using (var dataInputStream = new EndianReader(inputStream, Endianness.BigEndian))
            {
                return RawMessageCodec.DecodeKeySignResponse(ParseMessage(dataInputStream).Segment());
            }
        }

        /// <exception cref="System.IO.IOException"/>
        static byte[] ParseMessage(EndianReader dataInputStream)
        {
            var size = dataInputStream.ReadUInt16();
            return dataInputStream.ReadBytes(size);
        }
    }
}
