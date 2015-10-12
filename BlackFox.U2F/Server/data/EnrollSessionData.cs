// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

namespace BlackFox.U2F.Server.data
{
	[System.Serializable]
	public class EnrollSessionData
	{
		private const long SERIAL_VERSION_UID = 1750990095756334568L;

		private readonly string accountName;

		private readonly byte[] challenge;

		private readonly string appId;

		public EnrollSessionData(string accountName, string appId, byte[] challenge)
		{
			this.accountName = accountName;
			this.challenge = challenge;
			this.appId = appId;
		}

		public virtual string GetAccountName()
		{
			return accountName;
		}

		public virtual byte[] GetChallenge()
		{
			return challenge;
		}

		public virtual string GetAppId()
		{
			return appId;
		}
	}
}
