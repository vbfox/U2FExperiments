using BlackFox.U2F.Gnubby.Messages;
using JetBrains.Annotations;

namespace BlackFox.U2F.Key
{
	public interface IU2FKey
	{
		[NotNull]
		KeyRegisterResponse Register([NotNull] KeyRegisterRequest keyRegisterRequest);

        [NotNull]
        KeySignResponse Authenticate([NotNull] KeySignRequest keySignRequest);
	}
}
