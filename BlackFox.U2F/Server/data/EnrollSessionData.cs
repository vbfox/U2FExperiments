namespace BlackFox.U2F.Server.data
{
	public class EnrollSessionData
	{
		private const long SERIAL_VERSION_UID = 1750990095756334568L;

		private readonly string accountName;

		private readonly byte[] challenge;

		private readonly string appId;

		public EnrollSessionData(string accountName, string appId, byte[] challenge)
		{
			this.accountName = accountName;
			this.challenge = challenge;
			this.appId = appId;
		}

		public string GetAccountName()
		{
			return accountName;
		}

		public byte[] GetChallenge()
		{
			return challenge;
		}

		public string GetAppId()
		{
			return appId;
		}
	}
}
