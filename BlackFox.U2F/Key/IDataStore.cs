namespace BlackFox.U2F.Key
{
	public interface IDataStore
	{
		void StoreKeyPair(byte[] keyHandle, java.security.KeyPair keyPair);

		java.security.KeyPair GetKeyPair(byte[] keyHandle);

		int IncrementCounter();
	}
}
