namespace BlackFox.U2F.Client
{
	public interface IOldU2FClient
	{
		/// <exception cref="U2FException"/>
		void Register(string origin, string accountName);

		/// <exception cref="U2FException"/>
		void Authenticate(string origin, string accountName);
	}
}
