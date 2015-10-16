using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlackFox.U2F.Codec
{
    public class ClientDataCodec
    {
        public const string RequestTypeRegister = "navigator.id.finishEnrollment";

        public const string RequestTypeAuthenticate = "navigator.id.getAssertion";

        public const string JsonPropertyRequestType = "typ";

        public const string JsonPropertyServerChallengeBase64 = "challenge";

        public const string JsonPropertyServerOrigin = "origin";

        public const string JsonPropertyChannelId = "cid_pubkey";

        // Constants for ClientData.typ
        // Constants for building ClientData.challenge
        /// <summary>Computes ClientData.challenge</summary>
        public static string EncodeClientData(string requestType, string serverChallengeBase64, string origin,
            JObject jsonChannelId)
        {
            var browserData = new JObject
            {
                { JsonPropertyRequestType, requestType },
                { JsonPropertyServerChallengeBase64, serverChallengeBase64 },
                { JsonPropertyChannelId, jsonChannelId },
                { JsonPropertyServerOrigin, origin }
            };
            return browserData.ToString(Formatting.None);
        }
    }
}
