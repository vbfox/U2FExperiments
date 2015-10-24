using Newtonsoft.Json.Linq;

namespace BlackFox.U2F.GnubbyApi
{
    public interface ISender
    {
        string Origin { get; }

        /// <summary>
        /// TLS Channel ID of the connection used with the server if available
        /// </summary>
        JObject ChannelId { get;}
    }
}