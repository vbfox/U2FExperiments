namespace BlackFox.U2F.Key.messages
{
	public class AuthenticateRequest : IU2FRequest
	{
	    public const byte CheckOnly = 0x07;

	    public const byte UserPresenceSign = 0x03;

	    public AuthenticateRequest(byte control, byte[] challengeSha256, byte[] applicationSha256
			, byte[] keyHandle)
		{
			Control = control;
			ChallengeSha256 = challengeSha256;
			ApplicationSha256 = applicationSha256;
			KeyHandle = keyHandle;
		}

	    /// <summary>
	    /// The FIDO Client will set the control byte to one of the following values:
	    /// 0x07 ("check-only")
	    /// 0x03 ("enforce-user-presence-and-sign")
	    /// </summary>
	    public byte Control { get; }

	    /// <summary>
	    /// The challenge parameter is the SHA-256 hash of the Client Data, a
	    /// stringified JSON datastructure that the FIDO Client prepares.
	    /// </summary>
	    /// <remarks>
	    /// The challenge parameter is the SHA-256 hash of the Client Data, a
	    /// stringified JSON datastructure that the FIDO Client prepares. Among other
	    /// things, the Client Data contains the challenge from the relying party
	    /// (hence the name of the parameter). See below for a detailed explanation of
	    /// Client Data.
	    /// </remarks>
	    public byte[] ChallengeSha256 { get; }

	    /// <summary>
	    /// The application parameter is the SHA-256 hash of the application identity
	    /// of the application requesting the registration
	    /// </summary>
	    public byte[] ApplicationSha256 { get; }

	    /// <summary>The key handle obtained during registration.</summary>
	    public byte[] KeyHandle { get; }
	}
}
