using System.Text;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.messages;
using BlackFox.U2F.Server;
using BlackFox.U2F.Server.messages;
using BlackFox.U2F.Utils;
using Newtonsoft.Json.Linq;
using NodaTime;

namespace BlackFox.U2F.Client.impl
{
    public class U2FClientReferenceImpl : IU2FClient
    {
        readonly IOriginVerifier appIdVerifier;
        readonly IChannelIdProvider channelIdProvider;
        readonly IClock clock;
        readonly IClientCrypto crypto;
        readonly IU2FKey key;
        readonly IU2FServer server;

        public U2FClientReferenceImpl(IClientCrypto crypto, IOriginVerifier appIdVerifier,
            IChannelIdProvider channelIdProvider, IU2FServer server, IU2FKey key, IClock clock)
        {
            this.crypto = crypto;
            this.appIdVerifier = appIdVerifier;
            this.channelIdProvider = channelIdProvider;
            this.server = server;
            this.key = key;
            this.clock = clock;
        }

        public void Register(string origin, string accountName)
        {
            var registrationRequest = server.GetRegistrationRequest(accountName, origin);
            if (!registrationRequest.Version.Equals(U2FConsts.U2Fv2))
            {
                throw new U2FException($"Unsupported protocol version: {registrationRequest.Version}");
            }
            appIdVerifier.ValidateOrigin(registrationRequest.AppId, origin);
            var channelIdJson = channelIdProvider.GetJsonChannelId();
            string clientData;
            var registerRequest = RegistrationRequestToRegisterRequest(origin, registrationRequest, channelIdJson, out clientData, crypto);
            var registerResponse = key.Register(registerRequest);
            var rawRegisterResponse = RawMessageCodec.EncodeRegisterResponse(registerResponse);
            var rawRegisterResponseBase64 = WebSafeBase64Converter.ToBase64String(rawRegisterResponse);
            var clientDataBase64 = WebSafeBase64Converter.ToBase64String(Encoding.UTF8.GetBytes(clientData));
            server.ProcessRegistrationResponse(
                new RegistrationResponse(rawRegisterResponseBase64, clientDataBase64, registrationRequest.SessionId),
                clock.Now.ToUnixTimeMilliseconds());
        }

        public void Authenticate(string origin, string accountName)
        {
            // the key can be used to sign any of the requests - we're gonna sign the first one.
            var signRequest = server.GetSignRequests(accountName, origin)[0];
            if (!signRequest.Version.Equals(U2FConsts.U2Fv2))
            {
                throw new U2FException($"Unsupported protocol version: {signRequest.Version}");
            }
            appIdVerifier.ValidateOrigin(signRequest.AppId, origin);
            var channelIdJson = channelIdProvider.GetJsonChannelId();
            string clientData;
            var authenticateRequest = SignRequestToAuthenticateRequest(origin, signRequest, channelIdJson, out clientData, crypto);

            var authenticateResponse = key.Authenticate(authenticateRequest);
            var rawAuthenticateResponse = RawMessageCodec.EncodeAuthenticateResponse(authenticateResponse);
            var rawAuthenticateResponse64 = WebSafeBase64Converter.ToBase64String(rawAuthenticateResponse);
            var clientDataBase64 = WebSafeBase64Converter.ToBase64String(Encoding.UTF8.GetBytes(clientData));
            server.ProcessSignResponse(new SignResponse(clientDataBase64, rawAuthenticateResponse64,
                signRequest.Challenge, signRequest.SessionId, signRequest.AppId));
        }

        public static AuthenticateRequest SignRequestToAuthenticateRequest(string origin, SignRequest signRequest, JObject channelIdJson,
            out string clientData, IClientCrypto crypto)
        {
            clientData = ClientDataCodec.EncodeClientData(ClientDataCodec.RequestTypeAuthenticate, signRequest.Challenge,
                origin, channelIdJson);
            var clientDataSha256 = crypto.ComputeSha256(clientData);
            var appIdSha256 = crypto.ComputeSha256(signRequest.AppId);
            var keyHandle = WebSafeBase64Converter.FromBase64String(signRequest.KeyHandle);
            return new AuthenticateRequest(U2FVersion.V2,
                clientDataSha256, appIdSha256, keyHandle);
        }

        public static RegisterRequest RegistrationRequestToRegisterRequest(string origin,
            RegistrationRequest registrationRequest, JObject channelIdJson, out string clientData,
            IClientCrypto crypto)
        {
            clientData = ClientDataCodec.EncodeClientData(ClientDataCodec.RequestTypeRegister, registrationRequest.Challenge,
                origin, channelIdJson);
            var clientDataSha256 = crypto.ComputeSha256(clientData);
            var appIdSha256 = crypto.ComputeSha256(registrationRequest.AppId);
            return new RegisterRequest(appIdSha256, clientDataSha256);
        }
    }
}
