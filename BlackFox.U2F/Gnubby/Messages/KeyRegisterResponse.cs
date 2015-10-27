using System.Linq;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Gnubby.Messages
{
    public class KeyRegisterResponse : IU2FResponse
    {
        public KeyRegisterResponse(byte[] userPublicKey, byte[] keyHandle, X509Certificate attestationCertificate,
            byte[] signature)
        {
            UserPublicKey = userPublicKey;
            KeyHandle = keyHandle;
            AttestationCertificate = attestationCertificate;
            Signature = signature;
        }

        /// <summary>
        ///     This is the (uncompressed) x,y-representation of a curve point on the P-256
        ///     NIST elliptic curve.
        /// </summary>
        public byte[] UserPublicKey { get; }

        /// <summary>
        ///     This a handle that allows the U2F token to identify the generated key pair.
        /// </summary>
        /// <remarks>
        ///     This a handle that allows the U2F token to identify the generated key pair.
        ///     U2F tokens MAY wrap the generated private key and the application id it was
        ///     generated for, and output that as the key handle.
        /// </remarks>
        public byte[] KeyHandle { get; }

        /// <summary>This is a X.509 certificate.</summary>
        public X509Certificate AttestationCertificate { get; }

        /// <summary>This is a ECDSA signature (on P-256)</summary>
        public byte[] Signature { get; }

        protected bool Equals(KeyRegisterResponse other)
        {
            return UserPublicKey.SequenceEqual(other.UserPublicKey) && KeyHandle.SequenceEqual(other.KeyHandle) &&
                   Equals(AttestationCertificate, other.AttestationCertificate) &&
                   Signature.SequenceEqual(other.Signature);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((KeyRegisterResponse) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (UserPublicKey != null ? UserPublicKey.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (KeyHandle != null ? KeyHandle.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (AttestationCertificate != null ? AttestationCertificate.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Signature != null ? Signature.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}