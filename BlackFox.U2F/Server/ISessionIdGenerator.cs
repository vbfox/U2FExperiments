namespace BlackFox.U2F.Server
{
	public interface ISessionIdGenerator
	{
		string GenerateSessionId(string accountName);
	}
}
