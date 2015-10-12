namespace BlackFox.U2F.Server.messages
{
    public class SignResponse
    {
        public SignResponse(string bd, string sign, string challenge, string sessionId, string
            appId)
        {
            Bd = bd;
            Sign = sign;
            Challenge = challenge;
            SessionId = sessionId;
            AppId = appId;
        }

        /// <summary>websafe-base64(client data)</summary>
        public string Bd { get; }

        /// <summary>websafe-base64(raw response from U2F device)</summary>
        public string Sign { get; }

        /// <summary>challenge originally passed</summary>
        public string Challenge { get; }

        /// <summary>session id originally passed</summary>
        public string SessionId { get; }

        /// <summary>application id originally passed</summary>
        public string AppId { get; }
    }
}
