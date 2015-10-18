namespace BlackFox.U2F.Server.data
{
	public class EnrollSessionData
	{
	    public EnrollSessionData(string accountName, string appId, byte[] challenge)
		{
			AccountName = accountName;
			Challenge = challenge;
			AppId = appId;
		}

	    public string AccountName { get; }

	    public byte[] Challenge { get; }

	    public string AppId { get; }
	}
}
