namespace BlackFox.U2F.Client
{
	public interface IClientCrypto
	{
		/// <exception cref="U2FException"/>
		byte[] ComputeSha256(string message);
	}
}
