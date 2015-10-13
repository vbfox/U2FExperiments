namespace BlackFox.U2F.Server.data
{
	public class SignSessionData : EnrollSessionData
	{
		private const long SERIAL_VERSION_UID = -1374014642398686120L;

		private readonly byte[] publicKey;

		public SignSessionData(string accountName, string appId, byte[] challenge, byte[]
			 publicKey)
			: base(accountName, appId, challenge)
		{
			this.publicKey = publicKey;
		}

		public byte[] GetPublicKey()
		{
			return publicKey;
		}
	}
}
