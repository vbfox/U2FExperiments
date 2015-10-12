namespace BlackFox.U2F.Client
{
	public interface ICrypto
	{
		/// <exception cref="U2FException"/>
		byte[] ComputeSha256(string message);
	}
}
