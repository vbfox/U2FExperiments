using System.IO;
using BlackFox.U2F.Key.messages;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Codec
{
    /// <summary>Raw message formats, as per FIDO U2F: Raw Message Formats - Draft 4</summary>
    public class RawMessageCodec
    {
        public const byte RegistrationReservedByteValue = 0x05;

        public const byte RegistrationSignedReservedByteValue = 0x00;

        public static byte[] EncodeRegisterRequest(RegisterRequest registerRequest)
        {
            var appIdSha256 = registerRequest.ApplicationSha256;
            var challengeSha256 = registerRequest.ChallengeSha256;
            var result = new byte[appIdSha256.Length + challengeSha256.Length];
            using (var writer = new BinaryWriter(new MemoryStream(result)))
            {
                writer.Write(challengeSha256);
                writer.Write(appIdSha256);
            }
            return result;
        }

        /// <exception cref="U2FException"/>
        public static RegisterRequest DecodeRegisterRequest(byte[] data)
        {
            try
            {
                using (var inputStream = new BinaryReader(new MemoryStream(data)))
                {
                    var challengeSha256 = inputStream.ReadBytes(32);
                    var appIdSha256 = inputStream.ReadBytes(32);
                    if (inputStream.BaseStream.Position != inputStream.BaseStream.Length)
                    {
                        throw new U2FException("Message ends with unexpected data");
                    }
                    return new RegisterRequest(appIdSha256, challengeSha256);
                }
            }
            catch (IOException e)
            {
                throw new U2FException("Error when parsing raw RegistrationResponse", e);
            }
        }

        /// <exception cref="U2FException"/>
        public static byte[] EncodeRegisterResponse(RegisterResponse
            registerResponse)
        {
            var userPublicKey = registerResponse.UserPublicKey;
            var keyHandle = registerResponse.KeyHandle;
            var attestationCertificate = registerResponse.AttestationCertificate;
            var signature = registerResponse.Signature;
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
            using (var writer = new BinaryWriter(new MemoryStream(result)))
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
        public static RegisterResponse DecodeRegisterResponse(byte[] data)
        {
            try
            {
                using (var inputStream = new BinaryReader(new MemoryStream(data)))
                {
                    var reservedByte = inputStream.ReadByte();
                    var userPublicKey = inputStream.ReadBytes(65);
                    var keyHandleSize = inputStream.ReadByte();
                    var keyHandle = inputStream.ReadBytes(keyHandleSize);

                    var parser = new X509CertificateParser();
                    var attestationCertificate = parser.ReadCertificate(inputStream.BaseStream);

                    var signatureSize = (int) (inputStream.BaseStream.Length - inputStream.BaseStream.Position);
                    var signature = inputStream.ReadBytes(signatureSize);
                    if (reservedByte != RegistrationReservedByteValue)
                    {
                        throw new U2FException(
                            $"Incorrect value of reserved byte. Expected: {RegistrationReservedByteValue}. Was: {reservedByte}");
                    }
                    return new RegisterResponse(userPublicKey, keyHandle,
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
        public static byte[] EncodeAuthenticateRequest(AuthenticateRequest authenticateRequest)
        {
            var controlByte = authenticateRequest.Control;
            var appIdSha256 = authenticateRequest.ApplicationSha256;
            var challengeSha256 = authenticateRequest.ChallengeSha256;
            var keyHandle = authenticateRequest.KeyHandle;
            if (keyHandle.Length > 255)
            {
                throw new U2FException("keyHandle length cannot be longer than 255 bytes!");
            }
            var result = new byte[1 + appIdSha256.Length + challengeSha256.Length + 1 + keyHandle.Length];
            using (var writer = new BinaryWriter(new MemoryStream(result)))
            {
                writer.Write(controlByte);
                writer.Write(challengeSha256);
                writer.Write(appIdSha256);
                writer.Write((byte)keyHandle.Length);
                writer.Write(keyHandle);
            }
            return result;
        }

        /// <exception cref="U2FException"/>
        public static AuthenticateRequest DecodeAuthenticateRequest
            (byte[] data)
        {
            try
            {
                using (var inputStream = new BinaryReader(new MemoryStream(data)))
                {
                    var controlByte = inputStream.ReadByte();
                    var challengeSha256 = inputStream.ReadBytes(32);
                    var appIdSha256 = inputStream.ReadBytes(32);
                    var keyHandleSize = inputStream.ReadByte();
                    var keyHandle = inputStream.ReadBytes(keyHandleSize);
                    if (inputStream.BaseStream.Position != inputStream.BaseStream.Length)
                    {
                        throw new U2FException("Message ends with unexpected data");
                    }
                    return new AuthenticateRequest(controlByte, challengeSha256, appIdSha256, keyHandle);
                }
            }
            catch (IOException e)
            {
                throw new U2FException("Error when parsing raw RegistrationResponse", e);
            }
        }

        /// <exception cref="U2FException"/>
        public static byte[] EncodeAuthenticateResponse(AuthenticateResponse
            authenticateResponse)
        {
            var userPresence = authenticateResponse.UserPresence;
            var counter = authenticateResponse.Counter;
            var signature = authenticateResponse.Signature;
            var result = new byte[1 + 4 + signature.Length];
            using (var writer = new BinaryWriter(new MemoryStream(result)))
            {
                writer.Write(userPresence);
                writer.Write(counter);
                writer.Write(signature);
            }
            return result;
        }

        /// <exception cref="U2FException"/>
        public static AuthenticateResponse DecodeAuthenticateResponse(byte[] data)
        {
            try
            {
                using (var inputStream = new BinaryReader(new MemoryStream(data)))
                {
                    var userPresence = inputStream.ReadByte();
                    var counter = inputStream.ReadInt32();
                    var signatureSize = (int)(inputStream.BaseStream.Length - inputStream.BaseStream.Position);
                    var signature = inputStream.ReadBytes(signatureSize);

                    return new AuthenticateResponse(userPresence, counter, signature);
                }
            }
            catch (IOException e)
            {
                throw new U2FException("Error when parsing rawSignData", e);
            }
        }

        public static byte[] EncodeRegistrationSignedBytes(byte[] applicationSha256, byte[] challengeSha256, byte[] keyHandle, byte[] userPublicKey)
        {
            var size = 1 + applicationSha256.Length + challengeSha256.Length + keyHandle.Length + userPublicKey.Length;
            var signedData = new byte[size];

            using (var writer = new BinaryWriter(new MemoryStream(signedData)))
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

        public static byte[] EncodeAuthenticateSignedBytes(byte[] applicationSha256, byte userPresence, int counter, byte[] challengeSha256)
        {
            var size = applicationSha256.Length + 1 + 4 + challengeSha256.Length;
            var signedData = new byte[size];
            using (var writer = new BinaryWriter(new MemoryStream(signedData)))
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
