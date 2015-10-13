using JetBrains.Annotations;

namespace BlackFox.U2F.Key
{
	public interface IKeyHandleGenerator
	{
		byte[] GenerateKeyHandle([NotNull] byte[] applicationSha256, [NotNull] ECKeyPair keyPair);
	}
}
