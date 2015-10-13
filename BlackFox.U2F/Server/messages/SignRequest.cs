namespace BlackFox.U2F.Server.messages
{
    public class SignRequest
    {
        public SignRequest(string version, string challenge, string appId, string keyHandle, string sessionId)
        {
            Version = version;
            Challenge = challenge;
            AppId = appId;
            KeyHandle = keyHandle;
            SessionId = sessionId;
        }

        /// <summary>Version of the protocol that the to-be-registered U2F token must speak.</summary>
        /// <remarks>
        ///     Version of the protocol that the to-be-registered U2F token must speak. For
        ///     the version of the protocol described herein, must be "U2F_V2"
        /// </remarks>
        public string Version { get; }

        /// <summary>The websafe-base64-encoded challenge.</summary>
        public string Challenge { get; }

        /// <summary>The application id that the RP would like to assert.</summary>
        /// <remarks>
        ///     The application id that the RP would like to assert. The U2F token will
        ///     enforce that the key handle provided above is associated with this
        ///     application id. The browser enforces that the calling origin belongs to the
        ///     application identified by the application id.
        /// </remarks>
        public string AppId { get; }

        /// <summary>
        ///     websafe-base64 encoding of the key handle obtained from the U2F token
        ///     during registration.
        /// </summary>
        public string KeyHandle { get; }

        /// <summary>A session id created by the RP.</summary>
        /// <remarks>
        ///     A session id created by the RP. The RP can opaquely store things like
        ///     expiration times for the sign-in session, protocol version used, public key
        ///     expected to sign the identity assertion, etc. The response from the API
        ///     will include the sessionId. This allows the RP to fire off multiple signing
        ///     requests, and associate the responses with the correct request
        /// </remarks>
        public string SessionId { get; }

        protected bool Equals(SignRequest other)
        {
            return string.Equals(Version, other.Version) && string.Equals(Challenge, other.Challenge) &&
                   string.Equals(AppId, other.AppId) && string.Equals(KeyHandle, other.KeyHandle) &&
                   string.Equals(SessionId, other.SessionId);
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
            return Equals((SignRequest) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Version != null ? Version.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Challenge != null ? Challenge.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (AppId != null ? AppId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (KeyHandle != null ? KeyHandle.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (SessionId != null ? SessionId.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}