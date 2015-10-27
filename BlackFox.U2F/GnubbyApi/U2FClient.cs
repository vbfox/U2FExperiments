using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Client;
using BlackFox.U2F.Client.impl;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;
using BlackFox.U2F.Server.messages;
using JetBrains.Annotations;

namespace BlackFox.U2F.GnubbyApi
{
    /// <summary>
    /// Represent operations that can be run on one or multiple keys
    /// </summary>
    public interface IKeyOperations
    {
        [NotNull, ItemNotNull]
        Task<SignOperationResult> SignAsync([NotNull] ICollection<KeySignRequest> requests,
            CancellationToken cancellationToken = default(CancellationToken), bool invididualAttestation = false);

        [NotNull, ItemNotNull]
        Task<RegisterOperationResult> RegisterAsync(ICollection<KeyRegisterRequest> registerRequests, ICollection<KeySignRequest> signRequests,
            CancellationToken cancellationToken = new CancellationToken());
    }

    /// <summary>
    /// An implementation of <see cref="IKeyOperations"/> that uses all keys returned by a <see cref="IKeyFactory"/>.
    /// </summary>
    class KeyOperations : IKeyOperations
    {
        private readonly IKeyFactory keyFactory;

        public KeyOperations([NotNull] IKeyFactory keyFactory)
        {
            if (keyFactory == null)
            {
                throw new ArgumentNullException(nameof(keyFactory));
            }

            this.keyFactory = keyFactory;
        }

        public Task<SignOperationResult> SignAsync(ICollection<KeySignRequest> requests, CancellationToken cancellationToken = new CancellationToken(),
            bool invididualAttestation = false)
        {
            var signer = new MultiKeyOperation<SignOperationResult>(
                keyFactory,
                (keyId, ct) => new SignOperation(keyId, requests).SignAsync(ct),
                r => r.IsSuccess
                );
            return signer.RunOperationAsync(cancellationToken);
        }

        public Task<RegisterOperationResult> RegisterAsync(ICollection<KeyRegisterRequest> registerRequests, ICollection<KeySignRequest> signRequests,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var signer = new MultiKeyOperation<RegisterOperationResult>(
                keyFactory,
                (keyId, ct) => new RegisterOperation(keyId, registerRequests).AuthenticateAsync(ct),
                r => r.IsSuccess
                );
            return signer.RunOperationAsync(cancellationToken);
        }
    }

    public interface IU2FClient
    {
        [NotNull, ItemCanBeNull]
        Task<SignResponse> Sign([NotNull] ICollection<SignRequest> requests,
            CancellationToken cancellationToken = default(CancellationToken));

        [NotNull, ItemCanBeNull]
        Task<RegisterResponse> Register([NotNull] ICollection<RegisterRequest> registrationRequests,
            [NotNull] ICollection<SignRequest> signRequests, CancellationToken cancellationToken = default(CancellationToken));
    }

    public class U2FClient : IU2FClient
    {
        public delegate Task<bool> CheckAppIdOrOrigin(string origin, ICollection<string> appIds, CancellationToken cancellationToken);

        public static readonly CheckAppIdOrOrigin NoAppIdOrOriginCheck = (o, a, ct) => TaskEx.FromResult(true);

        [NotNull] private readonly ISender sender;

        readonly IClientCrypto crypto;

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

        [NotNull]
        private readonly IKeyOperations keyOperations;

        public U2FClient([NotNull] ISender sender, [NotNull] IKeyFactory keyFactory)
            : this(sender, null, null, new KeyOperations(keyFactory), BouncyCastleClientCrypto.Instance)
        {
        }

        public U2FClient([NotNull] ISender sender, [CanBeNull] CheckAppIdOrOrigin originChecker,
            [CanBeNull] CheckAppIdOrOrigin appIdChecker, [NotNull] IKeyFactory keyFactory,
            [NotNull] IClientCrypto crypto)
            : this(sender, originChecker, appIdChecker, new KeyOperations(keyFactory), crypto)
        {
        }

        public U2FClient([NotNull] ISender sender, [NotNull] IKeyOperations keyOperations)
            : this(sender, null, null, keyOperations, BouncyCastleClientCrypto.Instance)
        {
        }

        public U2FClient([NotNull] ISender sender, [CanBeNull] CheckAppIdOrOrigin originChecker,
            [CanBeNull] CheckAppIdOrOrigin appIdChecker, [NotNull] IKeyOperations keyOperations,
            [NotNull] IClientCrypto crypto)
        {
            if (sender == null)
            {
                throw new ArgumentNullException(nameof(sender));
            }
            if (keyOperations == null)
            {
                throw new ArgumentNullException(nameof(keyOperations));
            }
            if (crypto == null)
            {
                throw new ArgumentNullException(nameof(crypto));
            }

            this.sender = sender;
            this.originChecker = originChecker ?? NoAppIdOrOriginCheck;
            this.appIdChecker = appIdChecker ?? NoAppIdOrOriginCheck;
            this.keyOperations = keyOperations;
            this.crypto = crypto;
        }

        private struct SignInfo
        {
            public string ClientDataBase64 { get; }
            public KeySignRequest KeySignRequest { get; }
            public SignRequest SignRequest { get; }

            public SignInfo(SignRequest signRequest, string clientDataBase64, KeySignRequest keySignRequest)
            {
                ClientDataBase64 = clientDataBase64;
                KeySignRequest = keySignRequest;
                SignRequest = signRequest;
            }
        }

        private SignInfo GenerateSignInfo(SignRequest signRequest)
        {
            var clientDataB64 = ClientDataCodec.EncodeClientData(ClientDataCodec.RequestTypeAuthenticate, signRequest.Challenge,
                sender.Origin, sender.ChannelId);
            var clientDataSha256 = crypto.ComputeSha256(clientDataB64);
            var appIdSha256 = crypto.ComputeSha256(signRequest.AppId);
            var keyHandle = WebSafeBase64Converter.FromBase64String(signRequest.KeyHandle);
            var authRequest = new KeySignRequest(U2FVersion.V2,
                clientDataSha256, appIdSha256, keyHandle);
            return new SignInfo(signRequest, clientDataB64, authRequest);
        }

        public async Task<SignResponse> Sign(ICollection<SignRequest> requests,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var appIds = requests.Select(r => r.AppId).Distinct().ToList();
            await CheckOriginAndAppIdsAsync(appIds, cancellationToken);

            var signInfos = requests.Select(GenerateSignInfo).ToDictionary(s => s.KeySignRequest);
            var keyRequests = signInfos.Keys.ToList();
            var result = await keyOperations.SignAsync(keyRequests, cancellationToken);
            if (!result.IsSuccess)
            {
                return null;
            }

            var signInfo = signInfos[result.Request];

            return new SignResponse(
                WebSafeBase64Converter.ToBase64String(signInfo.ClientDataBase64),
                WebSafeBase64Converter.ToBase64String(RawMessageCodec.EncodeKeySignResponse(result.Response)),
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
            public RegisterRequest RegisterRequest { get; }
            public KeyRegisterRequest KeyRegisterRequest { get; }

            public RegisterInfo(RegisterRequest registerRequest, string clientDataBase64, KeyRegisterRequest keyRegisterRequest)
            {
                RegisterRequest = registerRequest;
                ClientDataBase64 = clientDataBase64;
                KeyRegisterRequest = keyRegisterRequest;
            }
        }

        private RegisterInfo GenerateRegisterInfo(RegisterRequest registerRequest)
        {
            var clientDataB64 = ClientDataCodec.EncodeClientData(ClientDataCodec.RequestTypeRegister, registerRequest.Challenge,
                sender.Origin, sender.ChannelId);
            var clientDataSha256 = crypto.ComputeSha256(clientDataB64);
            var appIdSha256 = crypto.ComputeSha256(registerRequest.AppId);
            return new RegisterInfo(registerRequest, clientDataB64, new KeyRegisterRequest(appIdSha256, clientDataSha256));
        }

        public async Task<RegisterResponse> Register(ICollection<RegisterRequest> registrationRequests,
            ICollection<SignRequest> signRequests, CancellationToken cancellationToken = default(CancellationToken))
        {
            var appIds = registrationRequests
                .Select(r => r.AppId)
                .Concat(signRequests.Select(r => r.AppId))
                .Distinct()
                .ToList();
            await CheckOriginAndAppIdsAsync(appIds, cancellationToken);

            var signInfos = signRequests.Select(GenerateSignInfo).ToDictionary(s => s.KeySignRequest);
            var keySignRequests = signInfos.Keys.ToList();

            var registerInfos = registrationRequests.Select(GenerateRegisterInfo).ToDictionary(s => s.KeyRegisterRequest);
            var keyRegisterRequests = registerInfos.Keys.ToList();
            var result = await keyOperations.RegisterAsync(keyRegisterRequests, keySignRequests,
                cancellationToken);
            if (!result.IsSuccess)
            {
                return null;
            }

            var registerInfo = registerInfos[result.Request];

            return new RegisterResponse(
                WebSafeBase64Converter.ToBase64String(RawMessageCodec.EncodeKeyRegisterResponse(result.Response)),
                WebSafeBase64Converter.ToBase64String(registerInfo.ClientDataBase64),
                registerInfo.RegisterRequest.SessionId);
        }
    }
}