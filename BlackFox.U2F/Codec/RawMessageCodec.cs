using System;
using System.IO;
using BlackFox.Binary;
using BlackFox.U2F.Gnubby.Messages;
using JetBrains.Annotations;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Codec
{
    /// <summary>Raw message formats, as per FIDO U2F: Raw Message Formats - Draft 4</summary>
    public class RawMessageCodec
    {
        public const byte RegistrationReservedByteValue = 0x05;

        public const byte RegistrationSignedReservedByteValue = 0x00;

        public static byte[] EncodeKeyRegisterRequest([NotNull] KeyRegisterRequest keyRegisterRequest)
        {
            if (keyRegisterRequest == null)
            {
                throw new ArgumentNullException(nameof(keyRegisterRequest));
            }

            var appIdSha256 = keyRegisterRequest.ApplicationSha256;
            var challengeSha256 = keyRegisterRequest.ChallengeSha256;
            var result = new byte[appIdSha256.Length + challengeSha256.Length];
            using (var writer = new EndianWriter(new MemoryStream(result), Endianness.BigEndian))
            {
                writer.Write(challengeSha256);
                writer.Write(appIdSha256);
            }
            return result;
        }

        /// <exception cref="U2FException"/>
        public static KeyRegisterRequest DecodeKeyRegisterRequest([NotNull] byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                using (var inputStream = new EndianReader(new MemoryStream(data), Endianness.BigEndian))
                {
                    var challengeSha256 = inputStream.ReadBytes(32);
                    var appIdSha256 = inputStream.ReadBytes(32);
                    if (inputStream.BaseStream.Position != inputStream.BaseStream.Length)
                    {
                        throw new U2FException("Message ends with unexpected data");
                    }
                    return new KeyRegisterRequest(appIdSha256, challengeSha256);
                }
            }
            catch (IOException e)
            {
                throw new U2FException("Error when parsing raw RegistrationResponse", e);
            }
        }

        /// <exception cref="U2FException"/>
        public static byte[] EncodeKeyRegisterResponse([NotNull] KeyRegisterResponse keyRegisterResponse)
        {
            if (keyRegisterResponse == null)
            {
                throw new ArgumentNullException(nameof(keyRegisterResponse));
            }

            var userPublicKey = keyRegisterResponse.UserPublicKey;
            var keyHandle = keyRegisterResponse.KeyHandle;
            var attestationCertificate = keyRegisterResponse.AttestationCertificate;
            var signature = keyRegisterResponse.Signature;
            byte[] attestationCertificateBytes;
            try
            {
                attestationCertificateBytes = attestationCertificate.GetEncoded();
            }
            catch (CertificateEncodingException e)
            {
                throw new U2FException("Error when encoding attestation certificate.", e);
            }
            if (keyHandle.Length > 255)
            {
                throw new U2FException("keyHandle length cannot be longer than 255 bytes!"
                    );
            }
            var result = new byte[1 + userPublicKey.Length + 1 + keyHandle.Length + attestationCertificateBytes
                .Length + signature.Length];
            using (var writer = new EndianWriter(new MemoryStream(result), Endianness.BigEndian))
            {
                writer.Write(RegistrationReservedByteValue);
                writer.Write(userPublicKey);
                writer.Write((byte)keyHandle.Length);
                writer.Write(keyHandle);
                writer.Write(attestationCertificateBytes);
                writer.Write(signature);
            }
            return result;
        }

        /// <exception cref="U2FException"/>
        public static KeyRegisterResponse DecodeKeyRegisterResponse(ArraySegment<byte> data)
        {
            try
            {
                using (var stream = data.AsStream())
                using (var inputStream = new EndianReader(stream, Endianness.BigEndian))
                {
                    var reservedByte = inputStream.ReadByte();
                    var userPublicKey = inputStream.ReadBytes(65);
                    var keyHandleSize = inputStream.ReadByte();
                    var keyHandle = inputStream.ReadBytes(keyHandleSize);

                    var parser = new X509CertificateParser();
                    var attestationCertificate = parser.ReadCertificate(inputStream.BaseStream);

                    var signatureSize = (int)(inputStream.BaseStream.Length - inputStream.BaseStream.Position);
                    var signature = inputStream.ReadBytes(signatureSize);
                    if (reservedByte != RegistrationReservedByteValue)
                    {
                        throw new U2FException(
                            $"Incorrect value of reserved byte. Expected: {RegistrationReservedByteValue}. Was: {reservedByte}");
                    }
                    return new KeyRegisterResponse(userPublicKey, keyHandle,
                        attestationCertificate, signature);
                }
            }
            catch (IOException e)
            {
                throw new U2FException("Error when parsing raw RegistrationResponse", e);
            }
            catch (CertificateException e)
            {
                throw new U2FException("Error when parsing attestation certificate", e);
            }
        }

        /// <exception cref="U2FException"/>
        public static byte[] EncodeKeySignRequest([NotNull] KeySignRequest keySignRequest)
        {
            if (keySignRequest == null)
            {
                throw new ArgumentNullException(nameof(keySignRequest));
            }

            //var controlByte = authenticateRequest.Control;
            var appIdSha256 = keySignRequest.ApplicationSha256;
            var challengeSha256 = keySignRequest.ChallengeSha256;
            var keyHandle = keySignRequest.KeyHandle;
            if (keyHandle.Length > 255)
            {
                throw new U2FException("keyHandle length cannot be longer than 255 bytes!");
            }

            int size;
            switch (keySignRequest.Version)
            {
                case U2FVersion.V1:
                    size = appIdSha256.Length + challengeSha256.Length + keyHandle.Length;
                    break;
                case U2FVersion.V2:
                    size = appIdSha256.Length + challengeSha256.Length + keyHandle.Length + 1;
                    break;
                default:
                    throw new ArgumentException("Unknown version: " + keySignRequest.Version, nameof(keySignRequest));
            }

            var result = new byte[size];
            using (var writer = new EndianWriter(new MemoryStream(result), Endianness.BigEndian))
            {
                writer.Write(challengeSha256);
                writer.Write(appIdSha256);
                if (keySignRequest.Version == U2FVersion.V2)
                {
                    writer.Write((byte)keyHandle.Length);
                }
                writer.Write(keyHandle);
            }
            return result;
        }

        /// <exception cref="U2FException"/>
        public static KeySignRequest DecodeKeySignRequest([NotNull] byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            try
            {
                using (var inputStream = new EndianReader(new MemoryStream(data), Endianness.BigEndian))
                {
                    var challengeSha256 = inputStream.ReadBytes(32);
                    var appIdSha256 = inputStream.ReadBytes(32);
                    var keyHandleSize = inputStream.ReadByte();
                    var keyHandle = inputStream.ReadBytes(keyHandleSize);
                    if (inputStream.BaseStream.Position != inputStream.BaseStream.Length)
                    {
                        throw new U2FException("Message ends with unexpected data");
                    }
                    return new KeySignRequest(U2FVersion.V2, challengeSha256, appIdSha256, keyHandle);
                }
            }
            catch (IOException e)
            {
                throw new U2FException("Error when parsing raw RegistrationResponse", e);
            }
        }

        /// <exception cref="U2FException"/>
        public static byte[] EncodeKeySignResponse([NotNull] KeySignResponse keySignResponse)
        {
            if (keySignResponse == null)
            {
                throw new ArgumentNullException(nameof(keySignResponse));
            }

            var userPresence = keySignResponse.UserPresence;
            var counter = keySignResponse.Counter;
            var signature = keySignResponse.Signature;
            var result = new byte[1 + 4 + signature.Length];
            using (var writer = new EndianWriter(new MemoryStream(result), Endianness.BigEndian))
            {
                writer.Write(userPresence);
                writer.Write(counter);
                writer.Write(signature);
            }
            return result;
        }

        /// <exception cref="U2FException"/>
        public static KeySignResponse DecodeKeySignResponse(ArraySegment<byte> data)
        {
            try
            {
                using (var stream = data.AsStream())
                {
                    using (var inputStream = new EndianReader(stream, Endianness.BigEndian))
                    {
                        var userPresence = inputStream.ReadByte();
                        var counter = inputStream.ReadInt32();
                        var signatureSize = (int)(inputStream.BaseStream.Length - inputStream.BaseStream.Position);
                        var signature = inputStream.ReadBytes(signatureSize);

                        return new KeySignResponse(userPresence, counter, signature);
                    }
                }
            }
            catch (IOException e)
            {
                throw new U2FException("Error when parsing rawSignData", e);
            }
        }

        public static byte[] EncodeKeyRegisterSignedBytes(byte[] applicationSha256, byte[] challengeSha256, byte[] keyHandle, byte[] userPublicKey)
        {
            var size = 1 + applicationSha256.Length + challengeSha256.Length + keyHandle.Length + userPublicKey.Length;
            var signedData = new byte[size];

            using (var writer = new EndianWriter(new MemoryStream(signedData), Endianness.BigEndian))
            {
                writer.Write(RegistrationSignedReservedByteValue);
                writer.Write(applicationSha256);
                writer.Write(challengeSha256);
                writer.Write(keyHandle);
                writer.Write(userPublicKey);
            }

            // RFU
            return signedData;
        }

        public static byte[] EncodeKeySignSignedBytes(byte[] applicationSha256, byte userPresence, int counter, byte[] challengeSha256)
        {
            var size = applicationSha256.Length + 1 + 4 + challengeSha256.Length;
            var signedData = new byte[size];
            using (var writer = new EndianWriter(new MemoryStream(signedData), Endianness.BigEndian))
            {
                writer.Write(applicationSha256);
                writer.Write(userPresence);
                writer.Write(counter);
                writer.Write(challengeSha256);
            }
            return signedData;
        }
    }
}
