using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Tests
{
	public class TestUtils
	{
	    public static byte[] ParseHex(string hexEncoded)
	    {
	        if (hexEncoded.Length%2 != 0)
	        {
	            throw new ArgumentException("Expected a even number of chars");
	        }

	        var byteCount = hexEncoded.Length/2;
	        var result = new byte[byteCount];
	        var buffer = new char[2];

            var reader = new StringReader(hexEncoded);
	        for (int i = 0; i < byteCount; i++)
	        {
	            reader.Read(buffer, 0, 2);
	            var str = new string(buffer);
	            result[i] = Convert.ToByte(str, 16);
	        }

	        return result;
		}

		public static byte[] ParseBase64(string base64Encoded)
		{
		    return WebSafeBase64Converter.FromBase64String(base64Encoded);
		}

		public static X509Certificate ParseCertificate(byte[] encodedDerCertificate)
		{
            var parser = new X509CertificateParser();
		    return parser.ReadCertificate(encodedDerCertificate);
		}

		public static X509Certificate ParseCertificate(string encodedDerCertificateHex)
		{
			return ParseCertificate(ParseHex(encodedDerCertificateHex));
		}

		public static X509Certificate ParseCertificateBase64(string encodedDerCertificate)
		{
			return ParseCertificate(ParseBase64(encodedDerCertificate));
		}

		public static ECPrivateKeyParameters ParsePrivateKey(string keyBytesHex)
		{
            var curve = SecNamedCurves.GetByName("secp256r1");
            var curveSpec = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            return new ECPrivateKeyParameters(new BigInteger(keyBytesHex, 16), curveSpec);
        }

		public static ECPublicKeyParameters ParsePublicKey(byte[] keyBytes)
		{
            var curve = SecNamedCurves.GetByName("secp256r1");
		    var point = curve.Curve.DecodePoint(keyBytes);
            var parameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);
            return new ECPublicKeyParameters(point, parameters);
		}

		public static byte[] ComputeSha256(byte[] bytes)
		{
            var sha256 = SHA256.Create();
            return sha256.ComputeHash(bytes);
		}

		public static byte[] ComputeSha256(string data)
		{
			return ComputeSha256(Encoding.UTF8.GetBytes(data));
		}
	}
}
