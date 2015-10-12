namespace BlackFox.U2F.Server.messages
{
    public class RegistrationResponse
    {
        /// <summary>websafe-base64(raw registration response message)</summary>
        readonly string registrationData;

        /// <summary>websafe-base64(UTF8(stringified(client data)))</summary>
        readonly string bd;

        /// <summary>session id originally passed</summary>
        readonly string sessionId;

        public RegistrationResponse(string registrationData, string bd, string sessionId)
        {
            this.registrationData = registrationData;
            this.bd = bd;
            this.sessionId = sessionId;
        }

        public string GetRegistrationData()
        {
            return registrationData;
        }

        public string GetBd()
        {
            return bd;
        }

        public string GetSessionId()
        {
            return sessionId;
        }
    }
}
