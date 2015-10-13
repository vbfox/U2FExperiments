using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Server.data
{
    public class SecurityKeyData
    {
        public SecurityKeyData(long enrollmentTime, byte[] keyHandle, byte[] publicKey, X509Certificate attestationCert,
            int counter)
            : this(enrollmentTime, null, keyHandle, publicKey, attestationCert, counter)
        {
        }

        public SecurityKeyData(long enrollmentTime, IList<SecurityKeyDataTransports> transports, byte[] keyHandle,
            byte[] publicKey, X509Certificate attestationCert, int counter)
        {
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
        /// True if both lists are null or if both lists contain the same transport values
        /// </returns>
        public static bool ContainSameTransports(IList<SecurityKeyDataTransports
            > transports1, IList<SecurityKeyDataTransports
                > transports2)
        {
            if (transports1 == null && transports2 == null)
            {
                return true;
            }
            if (transports1 == null || transports2 == null)
            {
                return false;
            }
            return transports1.containsAll(transports2) && transports2.containsAll(transports1
                );
        }

        public override string ToString()
        {
            return new StringBuilder()
                .AppendFormat("public_key: {0}\n", WebSafeBase64Converter.ToBase64String(PublicKey))
                .AppendFormat("key_handle: {0}\n", WebSafeBase64Converter.ToBase64String(KeyHandle))
                .AppendFormat("counter: {0}\n", Counter)
                .AppendFormat("attestation certificate:\n")
                .Append(AttestationCertificate)
                .AppendFormat("transports: {0}\n", string.Join(", ", Transports.Select(t => t.ToString())))
                .ToString();
        }
    }
}