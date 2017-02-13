using BlackFox.U2F.Gnubby.Messages;
using JetBrains.Annotations;

namespace BlackFox.U2F.Key
{
    /// <summary>
    /// A physical U2F Key device.
    /// </summary>
	public interface IU2FKey
	{
		[NotNull]
		KeyRegisterResponse Register([NotNull] KeyRegisterRequest keyRegisterRequest);

        [NotNull]
        KeySignResponse Authenticate([NotNull] KeySignRequest keySignRequest);
	}
}
