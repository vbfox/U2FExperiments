using Org.BouncyCastle.Crypto.Parameters;

namespace BlackFox.U2F.Key
{
	public interface ICrypto
	{
		/// <exception cref="U2FException"/>
		byte[] Sign(byte[] signedData, ECPrivateKeyParameters certificatePrivateKey);
	}
}
