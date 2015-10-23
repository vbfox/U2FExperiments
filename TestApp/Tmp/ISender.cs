using Newtonsoft.Json.Linq;

namespace U2FExperiments.Tmp
{
    interface ISender
    {
        string Origin { get; }

        /// <summary>
        /// TLS Channel ID of the connection used with the server if available
        /// </summary>
        JObject ChannelId { get;}
    }
}