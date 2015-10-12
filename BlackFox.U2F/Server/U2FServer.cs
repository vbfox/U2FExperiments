// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

using BlackFox.U2F.Server.data;
using BlackFox.U2F.Server.messages;

namespace BlackFox.U2F.Server
{
	public interface IU2FServer
	{
		// registration //
		/// <exception cref="U2FException"/>
		RegistrationRequest GetRegistrationRequest(string 
			accountName, string appId);

		/// <exception cref="U2FException"/>
		SecurityKeyData ProcessRegistrationResponse(RegistrationResponse
			 registrationResponse, long currentTimeInMillis);

		// authentication //
		/// <exception cref="U2FException"/>
		System.Collections.Generic.IList<SignRequest> GetSignRequest
			(string accountName, string appId);

		/// <exception cref="U2FException"/>
		SecurityKeyData ProcessSignResponse(SignResponse
			 signResponse);

		// token management //
		System.Collections.Generic.IList<SecurityKeyData> GetAllSecurityKeys
			(string accountName);

		/// <exception cref="U2FException"/>
		void RemoveSecurityKey(string accountName, byte[] publicKey);
	}
}
