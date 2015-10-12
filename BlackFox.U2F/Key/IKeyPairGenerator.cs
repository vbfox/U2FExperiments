namespace BlackFox.U2F.Key
{
	public interface IKeyPairGenerator
	{
		java.security.KeyPair GenerateKeyPair(byte[] applicationSha256, byte[] challengeSha256
			);

		byte[] EncodePublicKey(java.security.PublicKey publicKey);
	}
}
