using JetBrains.Annotations;
using Org.BouncyCastle.Crypto.Parameters;

namespace BlackFox.U2F.Key
{
	public interface IKeyPairGenerator
	{
		ECKeyPair GenerateKeyPair([NotNull] byte[] applicationSha256, [NotNull] byte[] challengeSha256);

		byte[] EncodePublicKey([NotNull] ECPublicKeyParameters publicKey);
	}
}
