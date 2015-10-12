// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

namespace BlackFox.U2F.Server.impl
{
	public class BouncyCastleCrypto : ICrypto
	{
		static BouncyCastleCrypto()
		{
			java.security.Security.addProvider(new org.bouncycastle.jce.provider.BouncyCastleProvider
				());
		}

		/// <exception cref="U2FException"/>
		public virtual bool verifySignature(rg.BouncyCastle.X509.X509Certificate attestationCertificate
			, byte[] signedBytes, byte[] signature)
		{
			return verifySignature(attestationCertificate.getPublicKey(), signedBytes, signature
				);
		}

		/// <exception cref="U2FException"/>
		public virtual bool verifySignature(java.security.PublicKey publicKey, byte[] signedBytes
			, byte[] signature)
		{
			try
			{
				java.security.Signature ecdsaSignature = java.security.Signature.getInstance("SHA256withECDSA"
					);
				ecdsaSignature.initVerify(publicKey);
				ecdsaSignature.update(signedBytes);
				return ecdsaSignature.verify(signature);
			}
			catch (java.security.InvalidKeyException e)
			{
				throw new U2FException("Error when verifying signature", e);
			}
			catch (java.security.SignatureException e)
			{
				throw new U2FException("Error when verifying signature", e);
			}
			catch (java.security.NoSuchAlgorithmException e)
			{
				throw new U2FException("Error when verifying signature", e);
			}
		}

		/// <exception cref="U2FException"/>
		public virtual java.security.PublicKey DecodePublicKey(byte[] encodedPublicKey)
		{
			try
			{
				org.bouncycastle.asn1.x9.X9ECParameters curve = org.bouncycastle.asn1.sec.SECNamedCurves
					.getByName("secp256r1");
				org.bouncycastle.math.ec.ECPoint point;
				try
				{
					point = curve.getCurve().decodePoint(encodedPublicKey);
				}
				catch (System.Exception e)
				{
					throw new U2FException("Couldn't parse user public key", e);
				}
				return java.security.KeyFactory.getInstance("ECDSA").generatePublic(new org.bouncycastle.jce.spec.ECPublicKeySpec
					(point, new org.bouncycastle.jce.spec.ECParameterSpec(curve.getCurve(), curve.getG
					(), curve.getN(), curve.getH())));
			}
			catch (java.security.spec.InvalidKeySpecException e)
			{
				throw new U2FException("Error when decoding public key", e);
			}
			catch (java.security.NoSuchAlgorithmException e)
			{
				throw new U2FException("Error when decoding public key", e);
			}
		}

		/// <exception cref="U2FException"/>
		public virtual byte[] ComputeSha256(byte[] bytes)
		{
			try
			{
				return java.security.MessageDigest.getInstance("SHA-256").digest(bytes);
			}
			catch (java.security.NoSuchAlgorithmException e)
			{
				throw new U2FException("Error when computing SHA-256", e);
			}
		}
	}
}
