using System.Text;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.messages;
using BlackFox.U2F.Server;
using BlackFox.U2F.Server.messages;

namespace BlackFox.U2F.Client.impl
{
    public class Iu2FClientReferenceImpl : IU2FClient
    {
        readonly ICrypto crypto;
        readonly IOriginVerifier appIdVerifier;
        readonly IChannelIdProvider channelIdProvider;
        readonly IU2FServer server;
        readonly IU2FKey key;

        public Iu2FClientReferenceImpl(ICrypto crypto, IOriginVerifier
            appIdVerifier, IChannelIdProvider channelIdProvider, IU2FServer
                server, IU2FKey key)
        {
            this.crypto = crypto;
            this.appIdVerifier = appIdVerifier;
            this.channelIdProvider = channelIdProvider;
            this.server = server;
            this.key = key;
        }

        /// <exception cref="U2FException"/>
        public virtual void Register(string origin, string accountName)
        {
            var registrationRequest = server.GetRegistrationRequest
                (accountName, origin);
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
            var clientData = ClientDataCodec.EncodeClientData(ClientDataCodec
                .RequestTypeRegister, serverChallengeBase64, origin, channelIdJson);
            var appIdSha256 = crypto.ComputeSha256(appId);
            var clientDataSha256 = crypto.ComputeSha256(clientData);
            var registerResponse = key.Register(new
                RegisterRequest(appIdSha256, clientDataSha256));
            var rawRegisterResponse = RawMessageCodec.EncodeRegisterResponse
                (registerResponse);
            var rawRegisterResponseBase64 = WebSafeBase64Converter.ToBase64String
                (rawRegisterResponse);
            var clientDataBase64 = WebSafeBase64Converter.ToBase64String
                (Encoding.UTF8.GetBytes(clientData));
            server.ProcessRegistrationResponse(new RegistrationResponse
                (rawRegisterResponseBase64, clientDataBase64, sessionId), Sharpen.Runtime.currentTimeMillis
                    ());
        }

        /// <exception cref="U2FException"/>
        public virtual void Authenticate(string origin, string accountName)
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