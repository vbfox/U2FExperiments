using System.Linq;

namespace BlackFox.U2F.Key.messages
{
    /// <summary>
    /// Response sent by the key when an <see cref="AuthenticateRequest"/> is accepted and a signature is generated.
    /// </summary>
    public class AuthenticateResponse : IU2FResponse
    {
        public AuthenticateResponse(byte userPresence, int counter, byte[] signature)
        {
            UserPresence = userPresence;
            Counter = counter;
            Signature = signature;
        }

        /// <summary>Bit 0 is set to 1, which means that user presence was verified.</summary>
        /// <remarks>
        ///     Bit 0 is set to 1, which means that user presence was verified. (This
        ///     version of the protocol doesn't specify a way to request authentication
        ///     responses without requiring user presence.) A different value of Bit 0, as
        ///     well as Bits 1 through 7, are reserved for future use. The values of Bit 1
        ///     through 7 SHOULD be 0
        /// </remarks>
        public byte UserPresence { get; }

        /// <summary>
        ///     This is the big-endian representation of a counter value that the U2F token
        ///     increments every time it performs an authentication operation.
        /// </summary>
        public int Counter { get; }

        /// <summary>This is a ECDSA signature (on P-256)</summary>
        public byte[] Signature { get; }

        

        protected bool Equals(AuthenticateResponse other)
        {
            return UserPresence == other.UserPresence && Counter == other.Counter &&
                   Signature.SequenceEqual(other.Signature);
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
            return Equals((AuthenticateResponse) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = UserPresence.GetHashCode();
                hashCode = (hashCode*397) ^ Counter;
                hashCode = (hashCode*397) ^ (Signature != null ? Signature.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}