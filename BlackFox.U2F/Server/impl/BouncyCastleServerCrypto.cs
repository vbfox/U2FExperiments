using System;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math.EC;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Server.impl
{
    public class BouncyCastleServerCrypto : IServerCrypto
    {
        public bool VerifySignature(X509Certificate attestationCertificate, byte[] signedBytes, byte[] signature)
        {
            if (attestationCertificate == null)
            {
                throw new ArgumentNullException(nameof(attestationCertificate));
            }
            if (signedBytes == null)
            {
                throw new ArgumentNullException(nameof(signedBytes));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            var publicKey = attestationCertificate.GetPublicKey() as ECPublicKeyParameters;
            if (publicKey == null)
            {
                throw new U2FException("The certificate publickey isn't an Elliptic curve");
            }

            return VerifySignature(publicKey, signedBytes, signature);
        }

        public bool VerifySignature(ECPublicKeyParameters publicKey, byte[] signedBytes, byte[] signature)
        {
            if (publicKey == null)
            {
                throw new ArgumentNullException(nameof(publicKey));
            }
            if (signedBytes == null)
            {
                throw new ArgumentNullException(nameof(signedBytes));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }

            try
            {
                var ecdsaSignature = SignerUtilities.GetSigner("SHA-256withECDSA");
                ecdsaSignature.Init(false, publicKey);
                ecdsaSignature.BlockUpdate(signedBytes, 0, signedBytes.Length);
                return ecdsaSignature.VerifySignature(signature);
            }
            catch (SecurityUtilityException e)
            {
                throw new U2FException("Error when verifying signature", e);
            }
            catch (CryptoException e)
            {
                throw new U2FException("Error when verifying signature", e);
            }
        }

        public ECPublicKeyParameters DecodePublicKey(byte[] encodedPublicKey)
        {
            try
            {
                var curve = SecNamedCurves.GetByName("secp256r1");
                if (curve == null)
                {
                    throw new U2FException("Named curve 'secp256r1' isn't supported");
                }

                ECPoint point;
                try
                {
                    point = curve.Curve.DecodePoint(encodedPublicKey);
                }
                catch (Exception e)
                {
                    throw new U2FException("Couldn't parse user public key", e);
                }

                var parameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
                return new ECPublicKeyParameters(point, parameters);
            }
            catch (Exception e)
            {
                throw new U2FException("Error when decoding public key", e);
            }
        }

        public byte[] ComputeSha256(byte[] bytes)
        {
            try
            {
                return DigestUtilities.CalculateDigest("SHA-256", bytes);
            }
            catch (SecurityUtilityException e)
            {
                throw new U2FException("Cannot compute SHA-256", e);
            }
        }
    }
}