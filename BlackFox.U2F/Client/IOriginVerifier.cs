namespace BlackFox.U2F.Client
{
	public interface IOriginVerifier
	{
		/// <exception cref="U2FException"/>
		void ValidateOrigin(string appId, string origin);
	}
}
