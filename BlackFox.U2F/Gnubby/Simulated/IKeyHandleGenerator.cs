using JetBrains.Annotations;

namespace BlackFox.U2F.Gnubby.Simulated
{
	public interface IKeyHandleGenerator
	{
		byte[] GenerateKeyHandle([NotNull] byte[] applicationSha256, [NotNull] ECKeyPair keyPair);
	}
}
