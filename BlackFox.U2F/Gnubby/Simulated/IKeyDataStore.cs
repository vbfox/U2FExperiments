namespace BlackFox.U2F.Gnubby.Simulated
{
    public interface IKeyDataStore
	{
		void StoreKeyPair(byte[] keyHandle, ECKeyPair keyPair);

        ECKeyPair GetKeyPair(byte[] keyHandle);

		int IncrementCounter();
	}
}
