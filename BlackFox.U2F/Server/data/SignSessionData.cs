// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

namespace BlackFox.U2F.Server.data
{
	[System.Serializable]
	public class SignSessionData : EnrollSessionData
	{
		private const long SERIAL_VERSION_UID = -1374014642398686120L;

		private readonly byte[] publicKey;

		public SignSessionData(string accountName, string appId, byte[] challenge, byte[]
			 publicKey)
			: base(accountName, appId, challenge)
		{
			this.publicKey = publicKey;
		}

		public virtual byte[] GetPublicKey()
		{
			return publicKey;
		}
	}
}
