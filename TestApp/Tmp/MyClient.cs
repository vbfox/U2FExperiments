using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F;
using BlackFox.U2F.Client.impl;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Key.messages;
using BlackFox.U2F.Server.messages;
using JetBrains.Annotations;

namespace U2FExperiments.Tmp
{
    class MyClient
    {
        public delegate Task<bool> CheckAppIdOrOrigin(string origin, ICollection<string> appIds, CancellationToken cancellationToken);

        [NotNull] private readonly ISender sender;

        /// <summary>
        /// Check that an origin should be allowed to claim all the specified AppIds.
        /// </summary>
        /// <remarks>
        /// Should check if the eTLD+1 is the same (google.com) or if there is a redirection in place with the header :
        /// FIDO-AppID-Redirect-Authorized
        /// </remarks>
        [NotNull] private readonly CheckAppIdOrOrigin originChecker;
        /// <summary>
        /// Check that an origin accept the specified appids using the 'trustedFacets' in a JSON document
        /// </summary>
        [NotNull] private readonly CheckAppIdOrOrigin appIdChecker;

        [NotNull] private readonly IKeyFactory keyFactory;

        public MyClient([NotNull] ISender sender, [NotNull] IKeyFactory keyFactory)
            : this(sender, null, null, keyFactory)
        {
        }

        public MyClient([NotNull] ISender sender, [CanBeNull] CheckAppIdOrOrigin originChecker,
            [CanBeNull] CheckAppIdOrOrigin appIdChecker, [NotNull] IKeyFactory keyFactory)
        {
            this.sender = sender;
            this.originChecker = originChecker ?? ((o, a, ct) => Task.FromResult(true));
            this.appIdChecker = appIdChecker ?? ((o, a, ct) => Task.FromResult(true));
            this.keyFactory = keyFactory;
        }

        private struct SignInfo
        {
            public string ClientDataBase64 { get; }
            public AuthenticateRequest AuthenticateRequest { get; }
            public SignRequest SignRequest { get; }

            public SignInfo(SignRequest signRequest, string clientDataBase64, AuthenticateRequest authenticateRequest)
            {
                ClientDataBase64 = clientDataBase64;
                AuthenticateRequest = authenticateRequest;
                SignRequest = signRequest;
            }
        }

        private SignInfo GenerateSignInfo(SignRequest signRequest)
        {
            string clientDataB64;
            var authRequest = U2FClientReferenceImpl.SignRequestToAuthenticateRequest(sender.Origin,
                signRequest, sender.ChannelId,
                out clientDataB64, new BouncyCastleClientCrypto());
            return new SignInfo(signRequest, clientDataB64, authRequest);
        }

        public async Task<SignResponse> Sign(ICollection<SignRequest> requests,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var appIds = requests.Select(r => r.AppId).Distinct().ToList();
            await CheckOriginAndAppIdsAsync(appIds, cancellationToken);

            var signInfos = requests.Select(GenerateSignInfo).ToDictionary(s => s.AuthenticateRequest);
            var signer = new MultiSigner(keyFactory, signInfos.Keys.ToList());
            var result = await signer.SignAsync(cancellationToken);
            if (!result.IsSuccess)
            {
                return null;
            }

            var signInfo = signInfos[result.Request];

            return new SignResponse(
                WebSafeBase64Converter.ToBase64String(signInfo.ClientDataBase64),
                WebSafeBase64Converter.ToBase64String(RawMessageCodec.EncodeAuthenticateResponse(result.Response)),
                signInfo.SignRequest.Challenge,
                signInfo.SignRequest.SessionId,
                signInfo.SignRequest.AppId);
        }

        async Task CheckOriginAndAppIdsAsync(List<string> appIds, CancellationToken cancellationToken)
        {
            var originsOk = await originChecker(sender.Origin, appIds, cancellationToken);
            if (!originsOk)
            {
                throw new Exception("Bad origins for appid");
            }

            var appIdsOk = await appIdChecker(sender.Origin, appIds, cancellationToken);
            if (!appIdsOk)
            {
                throw new Exception("App IDs not allowed by this origin");
            }
        }

        private struct RegisterInfo
        {
            public string ClientDataBase64 { get; }
            public RegistrationRequest RegistrationRequest { get; }
            public RegisterRequest RegisterRequest { get; }

            public RegisterInfo(RegistrationRequest registrationRequest, string clientDataBase64, RegisterRequest registerRequest)
            {
                RegistrationRequest = registrationRequest;
                ClientDataBase64 = clientDataBase64;
                RegisterRequest = registerRequest;
            }
        }

        private RegisterInfo GenerateRegisterInfo(RegistrationRequest registrationRequest)
        {
            string clientDataB64;
            var registerRequest = U2FClientReferenceImpl.RegistrationRequestToRegisterRequest(sender.Origin,
                registrationRequest, sender.ChannelId,
                out clientDataB64, new BouncyCastleClientCrypto());
            return new RegisterInfo(registrationRequest, clientDataB64, registerRequest);
        }

        public async Task<RegistrationResponse> Register(ICollection<RegistrationRequest> registrationRequests,
            ICollection<SignRequest> signRequests, CancellationToken cancellationToken = default(CancellationToken))
        {
            var appIds = registrationRequests
                .Select(r => r.AppId)
                .Concat(signRequests.Select(r => r.AppId))
                .Distinct()
                .ToList();
            await CheckOriginAndAppIdsAsync(appIds, cancellationToken);

            var signInfos = registrationRequests.Select(GenerateRegisterInfo).ToDictionary(s => s.RegistrationRequest);

            return null;
        }
    }
}