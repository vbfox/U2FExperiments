// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

namespace BlackFox.U2F.Server
{
	public interface ICrypto
	{
		/// <exception cref="U2FException"/>
		bool verifySignature(Org.BouncyCastle.X509.X509Certificate attestationCertificate, byte
			[] signedBytes, byte[] signature);

		/// <exception cref="U2FException"/>
		bool verifySignature(java.security.PublicKey publicKey, byte[] signedBytes, byte[]
			 signature);

		/// <exception cref="U2FException"/>
		java.security.PublicKey DecodePublicKey(byte[] encodedPublicKey);

		/// <exception cref="U2FException"/>
		byte[] ComputeSha256(byte[] bytes);
	}
}
