using System;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.Binary;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Gnubby.Messages;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.impl;
using Common.Logging;
using JetBrains.Annotations;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Gnubby.Simulated
{
    public class SimulatedKey : IKey
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(U2FKeyReferenceImpl));

        private readonly ECPrivateKeyParameters certificatePrivateKey;
        private readonly IKeyCrypto crypto;
        private readonly IKeyDataStore dataStore;
        private readonly IKeyHandleGenerator keyHandleGenerator;
        private readonly IKeyPairGenerator keyPairGenerator;
        private readonly IUserPresenceVerifier userPresenceVerifier;
        private readonly X509Certificate vendorCertificate;

        public SimulatedKey([NotNull] X509Certificate vendorCertificate,
            [NotNull] ECPrivateKeyParameters certificatePrivateKey, [NotNull] IKeyPairGenerator keyPairGenerator,
            [NotNull] IKeyHandleGenerator keyHandleGenerator, [NotNull] IKeyDataStore dataStore,
            [NotNull] IUserPresenceVerifier userPresenceVerifier, [NotNull] IKeyCrypto crypto)
        {
            if (vendorCertificate == null)
            {
                throw new ArgumentNullException(nameof(vendorCertificate));
            }
            if (certificatePrivateKey == null)
            {
                throw new ArgumentNullException(nameof(certificatePrivateKey));
            }
            if (keyPairGenerator == null)
            {
                throw new ArgumentNullException(nameof(keyPairGenerator));
            }
            if (keyHandleGenerator == null)
            {
                throw new ArgumentNullException(nameof(keyHandleGenerator));
            }
            if (dataStore == null)
            {
                throw new ArgumentNullException(nameof(dataStore));
            }
            if (userPresenceVerifier == null)
            {
                throw new ArgumentNullException(nameof(userPresenceVerifier));
            }
            if (crypto == null)
            {
                throw new ArgumentNullException(nameof(crypto));
            }

            this.vendorCertificate = vendorCertificate;
            this.certificatePrivateKey = certificatePrivateKey;
            this.keyPairGenerator = keyPairGenerator;
            this.keyHandleGenerator = keyHandleGenerator;
            this.dataStore = dataStore;
            this.userPresenceVerifier = userPresenceVerifier;
            this.crypto = crypto;
        }

        public void Dispose()
        {
            // Nothing to do
        }

        public Task<KeyResponse<KeyRegisterResponse>> RegisterAsync(KeyRegisterRequest request, CancellationToken cancellationToken = new CancellationToken(),
            bool invididualAttestation = false)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            log.Info(">> register");

            var applicationSha256 = request.ApplicationSha256;
            var challengeSha256 = request.ChallengeSha256;

            log.Info(" -- Inputs --");
            log.Info("  applicationSha256: " + applicationSha256.ToHexString());
            log.Info("  challengeSha256: " + challengeSha256.ToHexString());

            var userPresent = userPresenceVerifier.VerifyUserPresence();
            if ((userPresent & UserPresenceVerifierConstants.UserPresentFlag) == 0)
            {
                return TestOfUserPresenceRequired<KeyRegisterResponse>();
            }

            var keyPair = keyPairGenerator.GenerateKeyPair(applicationSha256, challengeSha256);
            var keyHandle = keyHandleGenerator.GenerateKeyHandle(applicationSha256, keyPair);
            dataStore.StoreKeyPair(keyHandle, keyPair);

            var userPublicKey = keyPairGenerator.EncodePublicKey(keyPair.PublicKey);
            var signedData = RawMessageCodec.EncodeKeyRegisterSignedBytes(applicationSha256, challengeSha256, keyHandle,
                userPublicKey);

            log.Info("Signing bytes " + signedData.ToHexString());
            var signature = crypto.Sign(signedData, certificatePrivateKey);

            log.Info(" -- Outputs --");
            log.Info("  userPublicKey: " + userPublicKey.ToHexString());
            log.Info("  keyHandle: " + keyHandle.ToHexString());
            log.Info("  vendorCertificate: " + vendorCertificate);
            log.Info("  signature: " + signature.ToHexString());
            log.Info("<< register");

            var response = new KeyRegisterResponse(userPublicKey, keyHandle, vendorCertificate, signature);
            var responseData = RawMessageCodec.EncodeKeyRegisterResponse(response).Segment();
            var apdu = new ApduResponse(ApduResponseStatus.NoError, responseData);
            var keyResponse = new KeyResponse<KeyRegisterResponse>(apdu, response, KeyResponseStatus.Success);

            return TaskEx.FromResult(keyResponse);
        }

        private static Task<KeyResponse<TData>> TestOfUserPresenceRequired<TData>() where TData : class
        {
            return TaskEx.FromResult(KeyResponse<TData>.Empty(ApduResponseStatus.NoError,
                KeyResponseStatus.TestOfuserPresenceRequired));
        }

        public Task<KeyResponse<KeySignResponse>> SignAsync(KeySignRequest request, CancellationToken cancellationToken = new CancellationToken(),
            bool noWink = false)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            log.Info(">> authenticate");

            var applicationSha256 = request.ApplicationSha256;
            var challengeSha256 = request.ChallengeSha256;
            var keyHandle = request.KeyHandle;

            log.Info(" -- Inputs --");
            log.Info("  applicationSha256: " + applicationSha256.ToHexString());
            log.Info("  challengeSha256: " + challengeSha256.ToHexString());
            log.Info("  keyHandle: " + keyHandle.ToHexString());

            var keyPair = dataStore.GetKeyPair(keyHandle);
            var counter = dataStore.IncrementCounter();
            var userPresent = userPresenceVerifier.VerifyUserPresence();
            if ((userPresent & UserPresenceVerifierConstants.UserPresentFlag) == 0)
            {
                return TestOfUserPresenceRequired<KeySignResponse>();
            }

            var signedData = RawMessageCodec.EncodeKeySignSignedBytes(applicationSha256, userPresent, counter,
                challengeSha256);

            log.Info("Signing bytes " + signedData.ToHexString());
            var signature = crypto.Sign(signedData, keyPair.PrivateKey);

            log.Info(" -- Outputs --");
            log.Info("  userPresence: " + userPresent);
            log.Info("  counter: " + counter);
            log.Info("  signature: " + signature.ToHexString());
            log.Info("<< authenticate");

            var response = new KeySignResponse(userPresent, counter, signature);
            var responseData = RawMessageCodec.EncodeKeySignResponse(response).Segment();
            var apdu = new ApduResponse(ApduResponseStatus.NoError, responseData);
            var keyResponse = new KeyResponse<KeySignResponse>(apdu, response, KeyResponseStatus.Success);

            return TaskEx.FromResult(keyResponse);
        }

        public Task<KeyResponse<string>> GetVersionAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var keyResponse = new KeyResponse<string>(ApduResponse.Empty(ApduResponseStatus.NoError), U2FConsts.U2Fv2, KeyResponseStatus.Success);
            return TaskEx.FromResult(keyResponse);
        }

        public Task SyncAsync()
        {
            return TaskEx.FromResult(true);
        }
    }
}
