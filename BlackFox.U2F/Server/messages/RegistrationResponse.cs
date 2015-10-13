namespace BlackFox.U2F.Server.messages
{
    public class RegistrationResponse
    {
        public RegistrationResponse(string registrationData, string bd, string sessionId)
        {
            RegistrationData = registrationData;
            Bd = bd;
            SessionId = sessionId;
        }

        /// <summary>websafe-base64(raw registration response message)</summary>
        public string RegistrationData { get; }

        /// <summary>websafe-base64(UTF8(stringified(client data)))</summary>
        public string Bd { get; }

        /// <summary>session id originally passed</summary>
        public string SessionId { get; }
    }
}
