using System;
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
        private readonly IOriginVerifier appIdVerifier;
        private readonly IChannelIdProvider channelIdProvider;
        private readonly IClock clock;
        private readonly IClientCrypto crypto;
        private readonly IU2FKey key;
        private readonly IU2FServer server;

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

        /// <exception cref="U2FException" />
        public void Register(string origin, string accountName)
        {
            var registrationRequest = server.GetRegistrationRequest(accountName, origin);
            var version = registrationRequest.Version;
            var serverChallengeBase64 = registrationRequest.Challenge;
            var appId = registrationRequest.AppId;
            var sessionId = registrationRequest.SessionId;
            if (!version.Equals(U2FConsts.U2Fv2))
            {
                throw new U2FException($"Unsupported protocol version: {version}");
            }
            appIdVerifier.ValidateOrigin(appId, origin);
            var channelIdJson = channelIdProvider.GetJsonChannelId();
            var clientData = ClientDataCodec.EncodeClientData(ClientDataCodec.RequestTypeRegister, serverChallengeBase64,
                origin, channelIdJson);
            var appIdSha256 = crypto.ComputeSha256(appId);
            var clientDataSha256 = crypto.ComputeSha256(clientData);
            var registerResponse = key.Register(new RegisterRequest(appIdSha256, clientDataSha256));
            var rawRegisterResponse = RawMessageCodec.EncodeRegisterResponse(registerResponse);
            var rawRegisterResponseBase64 = WebSafeBase64Converter.ToBase64String(rawRegisterResponse);
            var clientDataBase64 = WebSafeBase64Converter.ToBase64String(Encoding.UTF8.GetBytes(clientData));
            server.ProcessRegistrationResponse(
                new RegistrationResponse(rawRegisterResponseBase64, clientDataBase64, sessionId),
                clock.Now.ToUnixTimeMilliseconds());
        }

        /// <exception cref="U2FException" />
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
                key.Authenticate(new AuthenticateRequest(UserPresenceVerifierConstants.UserPresentFlag, clientDataSha256,
                    appIdSha256, keyHandle));
            var rawAuthenticateResponse = RawMessageCodec.EncodeAuthenticateResponse(authenticateResponse);
            var rawAuthenticateResponse64 = WebSafeBase64Converter.ToBase64String(rawAuthenticateResponse);
            var clientDataBase64 = WebSafeBase64Converter.ToBase64String(Encoding.UTF8.GetBytes(clientData));
            server.ProcessSignResponse(new SignResponse(clientDataBase64, rawAuthenticateResponse64,
                serverChallengeBase64, sessionId, appId));
        }
    }
}