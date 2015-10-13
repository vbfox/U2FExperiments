namespace BlackFox.U2F.Server.messages
{
    public class SignResponse
    {
        public SignResponse(string bd, string sign, string challenge, string sessionId, string appId)
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

        protected bool Equals(SignResponse other)
        {
            return string.Equals(Bd, other.Bd) && string.Equals(Sign, other.Sign) &&
                   string.Equals(Challenge, other.Challenge) && string.Equals(SessionId, other.SessionId) &&
                   string.Equals(AppId, other.AppId);
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
            return Equals((SignResponse) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Bd != null ? Bd.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Sign != null ? Sign.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Challenge != null ? Challenge.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (SessionId != null ? SessionId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (AppId != null ? AppId.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}