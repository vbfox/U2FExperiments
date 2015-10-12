namespace BlackFox.U2F.Key.messages
{
	public class RegisterRequest : IU2FRequest
	{
	    public RegisterRequest(byte[] applicationSha256, byte[] challengeSha256)
		{
			ChallengeSha256 = challengeSha256;
			ApplicationSha256 = applicationSha256;
		}

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
	    public virtual byte[] ChallengeSha256 { get; }

	    /// <summary>
	    /// The application parameter is the SHA-256 hash of the application identity
	    /// of the application requesting the registration
	    /// </summary>
	    public virtual byte[] ApplicationSha256 { get; }
	}
}
