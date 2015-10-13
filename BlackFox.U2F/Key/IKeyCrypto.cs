using JetBrains.Annotations;
using Org.BouncyCastle.Crypto.Parameters;

namespace BlackFox.U2F.Key
{
	public interface IKeyCrypto
	{
		byte[] Sign([NotNull] byte[] signedData, [NotNull] ECPrivateKeyParameters certificatePrivateKey);
	}
}
