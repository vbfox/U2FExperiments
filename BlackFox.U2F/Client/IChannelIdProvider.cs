using Newtonsoft.Json.Linq;

namespace BlackFox.U2F.Client
{
	public interface IChannelIdProvider
	{
		JObject GetJsonChannelId();
	}
}
