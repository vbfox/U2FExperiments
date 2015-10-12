namespace BlackFox.U2F.Key
{
	public interface IKeyHandleGenerator
	{
		byte[] GenerateKeyHandle(byte[] applicationSha256, java.security.KeyPair keyPair);
	}
}
