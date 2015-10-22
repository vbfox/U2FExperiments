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
		System.Collections.Generic.IList<SignRequest> GetSignRequests
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
