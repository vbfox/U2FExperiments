using JetBrains.Annotations;

namespace BlackFox.U2F.Server
{
	public interface IChallengeGenerator
	{
	    [NotNull]
	    byte[] GenerateChallenge([NotNull] string accountName);
	}
}
