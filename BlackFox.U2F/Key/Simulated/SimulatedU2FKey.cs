using System;
using BlackFox.Binary;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;
using BlackFox.U2F.Gnubby.Simulated;
using Common.Logging;
using JetBrains.Annotations;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Key.Simulated
{
    public class SimulatedU2FKey : IU2FKey
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (SimulatedU2FKey));

        private readonly ECPrivateKeyParameters certificatePrivateKey;
        private readonly IKeyCrypto crypto;
        private readonly IKeyDataStore dataStore;
        private readonly IKeyHandleGenerator keyHandleGenerator;
        private readonly IKeyPairGenerator keyPairGenerator;
        private readonly IUserPresenceVerifier userPresenceVerifier;
        private readonly X509Certificate vendorCertificate;

        public SimulatedU2FKey([NotNull] X509Certificate vendorCertificate,
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

        /// <exception cref="U2FException" />
        public KeyRegisterResponse Register(KeyRegisterRequest keyRegisterRequest)
        {
            if (keyRegisterRequest == null)
            {
                throw new ArgumentNullException(nameof(keyRegisterRequest));
            }

            log.Info(">> register");

            var applicationSha256 = keyRegisterRequest.ApplicationSha256;
            var challengeSha256 = keyRegisterRequest.ChallengeSha256;

            log.Info(" -- Inputs --");
            log.Info("  applicationSha256: " + applicationSha256.ToHexString());
            log.Info("  challengeSha256: " + challengeSha256.ToHexString());

            var userPresent = userPresenceVerifier.VerifyUserPresence();
            if ((userPresent & UserPresenceVerifierConstants.UserPresentFlag) == 0)
            {
                throw new U2FException("Cannot verify user presence");
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

            return new KeyRegisterResponse(userPublicKey, keyHandle, vendorCertificate, signature);
        }

        /// <exception cref="U2FException" />
        public KeySignResponse Authenticate(KeySignRequest keySignRequest)
        {
            if (keySignRequest == null)
            {
                throw new ArgumentNullException(nameof(keySignRequest));
            }

            log.Info(">> authenticate");

            var applicationSha256 = keySignRequest.ApplicationSha256;
            var challengeSha256 = keySignRequest.ChallengeSha256;
            var keyHandle = keySignRequest.KeyHandle;

            log.Info(" -- Inputs --");
            log.Info("  applicationSha256: " + applicationSha256.ToHexString());
            log.Info("  challengeSha256: " + challengeSha256.ToHexString());
            log.Info("  keyHandle: " + keyHandle.ToHexString());

            var keyPair = dataStore.GetKeyPair(keyHandle);
            var counter = dataStore.IncrementCounter();
            var userPresence = userPresenceVerifier.VerifyUserPresence();
            var signedData = RawMessageCodec.EncodeKeySignSignedBytes(applicationSha256, userPresence, counter,
                challengeSha256);

            log.Info("Signing bytes " + signedData.ToHexString());
            var signature = crypto.Sign(signedData, keyPair.PrivateKey);

            log.Info(" -- Outputs --");
            log.Info("  userPresence: " + userPresence);
            log.Info("  counter: " + counter);
            log.Info("  signature: " + signature.ToHexString());
            log.Info("<< authenticate");

            return new KeySignResponse(userPresence, counter, signature);
        }
    }
}