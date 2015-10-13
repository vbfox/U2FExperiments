using BlackFox.U2F.Key.messages;
using JetBrains.Annotations;

namespace BlackFox.U2F.Key
{
	public interface IU2FKey
	{
		[NotNull]
		RegisterResponse Register([NotNull] RegisterRequest registerRequest);

        [NotNull]
        AuthenticateResponse Authenticate([NotNull] AuthenticateRequest authenticateRequest);
	}
}
