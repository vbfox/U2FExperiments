using System.Text;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.messages;
using BlackFox.U2F.Server;
using BlackFox.U2F.Server.messages;
using BlackFox.U2F.Utils;
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
            var clientData = ClientDataCodec.EncodeClientData(ClientDataCodec.RequestTypeRegister,
                registrationRequest.Challenge, origin, channelIdJson);
            var appIdSha256 = crypto.ComputeSha256(registrationRequest.AppId);
            var clientDataSha256 = crypto.ComputeSha256(clientData);
            var registerResponse = key.Register(new RegisterRequest(appIdSha256, clientDataSha256));
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
            var signRequest = server.GetSignRequest(accountName, origin)[0];
            var version = signRequest.Version;
            var appId = signRequest.AppId;
            var serverChallengeBase64 = signRequest.Challenge;
            var keyHandleBase64 = signRequest.KeyHandle;
            var sessionId = signRequest.SessionId;
            if (!version.Equals(U2FConsts.U2Fv2))
            {
                throw new U2FException($"Unsupported protocol version: {version}");
            }
            appIdVerifier.ValidateOrigin(appId, origin);
            var channelIdJson = channelIdProvider.GetJsonChannelId();
            var clientData = ClientDataCodec.EncodeClientData(ClientDataCodec.RequestTypeAuthenticate,
                serverChallengeBase64, origin, channelIdJson);
            var clientDataSha256 = crypto.ComputeSha256(clientData);
            var appIdSha256 = crypto.ComputeSha256(appId);
            var keyHandle = WebSafeBase64Converter.FromBase64String(keyHandleBase64);
            var authenticateResponse =
                key.Authenticate(new AuthenticateRequest(U2FVersion.V2, UserPresenceVerifierConstants.UserPresentFlag, clientDataSha256,
                    appIdSha256, keyHandle));
            var rawAuthenticateResponse = RawMessageCodec.EncodeAuthenticateResponse(authenticateResponse);
            var rawAuthenticateResponse64 = WebSafeBase64Converter.ToBase64String(rawAuthenticateResponse);
            var clientDataBase64 = WebSafeBase64Converter.ToBase64String(Encoding.UTF8.GetBytes(clientData));
            server.ProcessSignResponse(new SignResponse(clientDataBase64, rawAuthenticateResponse64,
                serverChallengeBase64, sessionId, appId));
        }
    }
}
