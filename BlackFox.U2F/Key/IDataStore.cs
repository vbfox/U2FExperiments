namespace BlackFox.U2F.Key
{
    public interface IDataStore
	{
		void StoreKeyPair(byte[] keyHandle, ECKeyPair keyPair);

        ECKeyPair GetKeyPair(byte[] keyHandle);

		int IncrementCounter();
	}
}
