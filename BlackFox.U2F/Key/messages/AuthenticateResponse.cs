namespace BlackFox.U2F.Key.messages
{
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
	    /// Bit 0 is set to 1, which means that user presence was verified. (This
	    /// version of the protocol doesn't specify a way to request authentication
	    /// responses without requiring user presence.) A different value of Bit 0, as
	    /// well as Bits 1 through 7, are reserved for future use. The values of Bit 1
	    /// through 7 SHOULD be 0
	    /// </remarks>
	    public virtual byte UserPresence { get; }

	    /// <summary>
	    /// This is the big-endian representation of a counter value that the U2F token
	    /// increments every time it performs an authentication operation.
	    /// </summary>
	    public virtual int Counter { get; }

	    /// <summary>This is a ECDSA signature (on P-256)</summary>
	    public virtual byte[] Signature { get; }
	}
}
