namespace BlackFox.U2F.Server
{
	public interface IChallengeGenerator
	{
		byte[] GenerateChallenge(string accountName);
	}
}
