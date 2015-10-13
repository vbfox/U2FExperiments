namespace BlackFox.U2F.Server.messages
{
    public class RegistrationRequest
    {
        public RegistrationRequest(string version, string challenge, string appId, string sessionId)
        {
            Version = version;
            Challenge = challenge;
            AppId = appId;
            SessionId = sessionId;
        }

        /// <summary>Version of the protocol that the to-be-registered U2F token must speak.</summary>
        /// <remarks>
        /// Version of the protocol that the to-be-registered U2F token must speak. For
        /// the version of the protocol described herein, must be "U2F_V2"
        /// </remarks>
        public string Version { get; }

        /// <summary>The websafe-base64-encoded challenge.</summary>
        public string Challenge { get; }

        /// <summary>The application id that the RP would like to assert.</summary>
        /// <remarks>
        /// The application id that the RP would like to assert. The U2F token will
        /// enforce that the key handle provided above is associated with this
        /// application id. The browser enforces that the calling origin belongs to the
        /// application identified by the application id.
        /// </remarks>
        public string AppId { get; }

        /// <summary>A session id created by the RP.</summary>
        /// <remarks>
        /// A session id created by the RP. The RP can opaquely store things like
        /// expiration times for the sign-in session, protocol version used, public key
        /// expected to sign the identity assertion, etc. The response from the API
        /// will include the sessionId. This allows the RP to fire off multiple signing
        /// requests, and associate the responses with the correct request
        /// </remarks>
        public string SessionId { get; }
    }
}
