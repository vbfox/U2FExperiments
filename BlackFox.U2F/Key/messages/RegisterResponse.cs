using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Key.messages
{
	public class RegisterResponse : IU2FResponse
	{
	    public RegisterResponse(byte[] userPublicKey, byte[] keyHandle, X509Certificate
			 attestationCertificate, byte[] signature)
		{
			UserPublicKey = userPublicKey;
			KeyHandle = keyHandle;
			AttestationCertificate = attestationCertificate;
			Signature = signature;
		}

	    /// <summary>
	    /// This is the (uncompressed) x,y-representation of a curve point on the P-256
	    /// NIST elliptic curve.
	    /// </summary>
	    public byte[] UserPublicKey { get; }

	    /// <summary>This a handle that allows the U2F token to identify the generated key pair.
	    /// 	</summary>
	    /// <remarks>
	    /// This a handle that allows the U2F token to identify the generated key pair.
	    /// U2F tokens MAY wrap the generated private key and the application id it was
	    /// generated for, and output that as the key handle.
	    /// </remarks>
	    public byte[] KeyHandle { get; }

	    /// <summary>This is a X.509 certificate.</summary>
	    public X509Certificate AttestationCertificate { get; }

	    /// <summary>This is a ECDSA signature (on P-256)</summary>
	    public byte[] Signature { get; }
	}
}
