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

        protected bool Equals(RegistrationResponse other)
        {
            return string.Equals(RegistrationData, other.RegistrationData) && string.Equals(Bd, other.Bd) &&
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
            return Equals((RegistrationResponse) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (RegistrationData != null ? RegistrationData.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Bd != null ? Bd.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (SessionId != null ? SessionId.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}