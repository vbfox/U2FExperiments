using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Server.data
{
    public class SecurityKeyData
    {
        public SecurityKeyData(long enrollmentTime, byte[] keyHandle, byte[] publicKey, X509Certificate attestationCert,
            int counter)
            : this(enrollmentTime, null, keyHandle, publicKey, attestationCert, counter
                )
        {
        }

        public SecurityKeyData(long enrollmentTime, [CanBeNull] IList<SecurityKeyDataTransports> transports,
            [NotNull] byte[] keyHandle, [NotNull] byte[] publicKey, [NotNull] X509Certificate attestationCert,
            int counter)
        {
            if (keyHandle == null)
            {
                throw new ArgumentNullException(nameof(keyHandle));
            }
            if (publicKey == null)
            {
                throw new ArgumentNullException(nameof(publicKey));
            }
            if (attestationCert == null)
            {
                throw new ArgumentNullException(nameof(attestationCert));
            }
            /* transports */
            EnrollmentTime = enrollmentTime;
            Transports = transports;
            KeyHandle = keyHandle;
            PublicKey = publicKey;
            AttestationCertificate = attestationCert;
            Counter = counter;
        }

        /// <summary>When these keys were created/enrolled with the relying party.</summary>
        public long EnrollmentTime { get; }

        public IList<SecurityKeyDataTransports> Transports { get; }
        public byte[] KeyHandle { get; }
        public byte[] PublicKey { get; }
        public X509Certificate AttestationCertificate { get; }
        public int Counter { get; set; }

        /// <summary>Compares the two Lists of Transports and says if they are equal.</summary>
        /// <param name="transports1">first List of Transports</param>
        /// <param name="transports2">second List of Transports</param>
        /// <returns>
        ///     True if both lists are null or if both lists contain the same transport values
        /// </returns>
        public static bool ContainSameTransports(IList<SecurityKeyDataTransports> transports1,
            IList<SecurityKeyDataTransports> transports2)
        {
            if (transports1 == null && transports2 == null)
            {
                return true;
            }
            if (transports1 == null || transports2 == null)
            {
                return false;
            }

            return transports1.All(transports2.Contains) && transports2.All(transports1.Contains);
        }

        public override string ToString()
        {
            return new StringBuilder()
                .AppendFormat("public_key: {0}\n", WebSafeBase64Converter.ToBase64String(PublicKey))
                .AppendFormat("key_handle: {0}\n", WebSafeBase64Converter.ToBase64String(KeyHandle))
                .AppendFormat("counter: {0}\n", Counter)
                .AppendFormat("attestation certificate:\n")
                .Append(AttestationCertificate)
                .AppendFormat("transports: {0}\n", Transports == null ? "" : string.Join(", ", Transports.Select(t => t.ToString())))
                .ToString();
        }

        protected bool Equals(SecurityKeyData other)
        {
            return EnrollmentTime == other.EnrollmentTime && ContainSameTransports(Transports, other.Transports) &&
                   KeyHandle.SequenceEqual(other.KeyHandle) && PublicKey.SequenceEqual(other.PublicKey) &&
                   Equals(AttestationCertificate, other.AttestationCertificate) && Counter == other.Counter;
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
            return Equals((SecurityKeyData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EnrollmentTime.GetHashCode();
                hashCode = (hashCode*397) ^ (Transports != null ? Transports.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (KeyHandle != null ? KeyHandle.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (PublicKey != null ? PublicKey.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (AttestationCertificate != null ? AttestationCertificate.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Counter;
                return hashCode;
            }
        }
    }
}