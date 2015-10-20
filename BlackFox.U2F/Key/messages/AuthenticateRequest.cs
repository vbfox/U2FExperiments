using System.Linq;

namespace BlackFox.U2F.Key.messages
{
    public class AuthenticateRequest : IU2FRequest
    {
        public const byte CheckOnly = 0x07;

        public const byte UserPresenceSign = 0x03;

        public U2FVersion Version { get; }

        public AuthenticateRequest(U2FVersion version, byte control, byte[] challengeSha256, byte[] applicationSha256, byte[] keyHandle)
        {
            Version = version;
            Control = control;
            ChallengeSha256 = challengeSha256;
            ApplicationSha256 = applicationSha256;
            KeyHandle = keyHandle;
        }

        /// <summary>
        ///     The FIDO Client will set the control byte to one of the following values:
        ///     0x07 ("check-only")
        ///     0x03 ("enforce-user-presence-and-sign")
        /// </summary>
        public byte Control { get; }

        /// <summary>
        ///     The challenge parameter is the SHA-256 hash of the Client Data, a
        ///     stringified JSON datastructure that the FIDO Client prepares.
        /// </summary>
        /// <remarks>
        ///     The challenge parameter is the SHA-256 hash of the Client Data, a
        ///     stringified JSON datastructure that the FIDO Client prepares. Among other
        ///     things, the Client Data contains the challenge from the relying party
        ///     (hence the name of the parameter). See below for a detailed explanation of
        ///     Client Data.
        /// </remarks>
        public byte[] ChallengeSha256 { get; }

        /// <summary>
        ///     The application parameter is the SHA-256 hash of the application identity
        ///     of the application requesting the registration
        /// </summary>
        public byte[] ApplicationSha256 { get; }

        /// <summary>The key handle obtained during registration.</summary>
        public byte[] KeyHandle { get; }

        protected bool Equals(AuthenticateRequest other)
        {
            return Control == other.Control && ChallengeSha256.SequenceEqual(other.ChallengeSha256) &&
                   ApplicationSha256.SequenceEqual(other.ApplicationSha256) && KeyHandle.SequenceEqual(other.KeyHandle);
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
            return Equals((AuthenticateRequest) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Control.GetHashCode();
                hashCode = (hashCode*397) ^ (ChallengeSha256 != null ? ChallengeSha256.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ApplicationSha256 != null ? ApplicationSha256.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (KeyHandle != null ? KeyHandle.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}