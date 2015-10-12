using BlackFox.U2F.Key.messages;

namespace BlackFox.U2F.Key
{
	public interface IU2FKey
	{
		/// <exception cref="U2FException"/>
		RegisterResponse Register(RegisterRequest
			 registerRequest);

		/// <exception cref="U2FException"/>
		AuthenticateResponse Authenticate(AuthenticateRequest
			 authenticateRequest);
	}
}
