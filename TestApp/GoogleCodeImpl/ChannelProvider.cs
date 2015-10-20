using BlackFox.U2F.Client;
using Newtonsoft.Json.Linq;

namespace U2FExperiments
{
    internal class ChannelProvider : IChannelIdProvider
    {
        public JObject GetJsonChannelId()
        {
            dynamic result = new JObject();
            result.Channel = 4; // chosen by fair dice roll. guaranteed to be random.
            return (JObject) result;
        }
    }
}