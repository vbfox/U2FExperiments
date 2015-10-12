using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace BlackFox.U2F.Key.impl
{
	public class BouncyCastleCrypto : ICrypto
	{
		/// <exception cref="U2FException"/>
		public virtual byte[] Sign(byte[] signedData, ECPrivateKeyParameters privateKey
			)
		{
			try
			{
			    var signature = SignerUtilities.GetSigner("SHA-256withECDSA");
				signature.Init(true, privateKey);
				signature.BlockUpdate(signedData, 0, signedData.Length);
                
                return signature.GenerateSignature();
			}
			catch (SecurityUtilityException e)
			{
				throw new U2FException("Error when signing", e);
			}
			catch (CryptoException e)
			{
				throw new U2FException("Error when signing", e);
			}
		}
	}
}
