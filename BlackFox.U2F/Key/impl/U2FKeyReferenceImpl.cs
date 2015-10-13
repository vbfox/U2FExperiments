using System;
using BlackFox.BinaryUtils;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key.messages;
using Common.Logging;
using JetBrains.Annotations;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Key.impl
{
    public class U2FKeyReferenceImpl : IU2FKey
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (U2FKeyReferenceImpl));

        private readonly ECPrivateKeyParameters certificatePrivateKey;
        private readonly IKeyCrypto crypto;
        private readonly IDataStore dataStore;
        private readonly IKeyHandleGenerator keyHandleGenerator;
        private readonly IKeyPairGenerator keyPairGenerator;
        private readonly IUserPresenceVerifier userPresenceVerifier;
        private readonly X509Certificate vendorCertificate;

        public U2FKeyReferenceImpl([NotNull] X509Certificate vendorCertificate,
            [NotNull] ECPrivateKeyParameters certificatePrivateKey, [NotNull] IKeyPairGenerator keyPairGenerator,
            [NotNull] IKeyHandleGenerator keyHandleGenerator, [NotNull] IDataStore dataStore,
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
        public RegisterResponse Register(RegisterRequest registerRequest)
        {
            if (registerRequest == null)
            {
                throw new ArgumentNullException(nameof(registerRequest));
            }

            log.Info(">> register");

            var applicationSha256 = registerRequest.ApplicationSha256;
            var challengeSha256 = registerRequest.ChallengeSha256;

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
            var signedData = RawMessageCodec.EncodeRegistrationSignedBytes(applicationSha256, challengeSha256, keyHandle,
                userPublicKey);

            log.Info("Signing bytes " + signedData.ToHexString());
            var signature = crypto.Sign(signedData, certificatePrivateKey);

            log.Info(" -- Outputs --");
            log.Info("  userPublicKey: " + userPublicKey.ToHexString());
            log.Info("  keyHandle: " + keyHandle.ToHexString());
            log.Info("  vendorCertificate: " + vendorCertificate);
            log.Info("  signature: " + signature.ToHexString());
            log.Info("<< register");

            return new RegisterResponse(userPublicKey, keyHandle, vendorCertificate, signature);
        }

        /// <exception cref="U2FException" />
        public AuthenticateResponse Authenticate(AuthenticateRequest authenticateRequest)
        {
            if (authenticateRequest == null)
            {
                throw new ArgumentNullException(nameof(authenticateRequest));
            }

            log.Info(">> authenticate");

            var control = authenticateRequest.Control;
            var applicationSha256 = authenticateRequest.ApplicationSha256;
            var challengeSha256 = authenticateRequest.ChallengeSha256;
            var keyHandle = authenticateRequest.KeyHandle;

            log.Info(" -- Inputs --");
            log.Info("  control: " + control);
            log.Info("  applicationSha256: " + applicationSha256.ToHexString());
            log.Info("  challengeSha256: " + challengeSha256.ToHexString());
            log.Info("  keyHandle: " + keyHandle.ToHexString());

            var keyPair = dataStore.GetKeyPair(keyHandle);
            var counter = dataStore.IncrementCounter();
            var userPresence = userPresenceVerifier.VerifyUserPresence();
            var signedData = RawMessageCodec.EncodeAuthenticateSignedBytes(applicationSha256, userPresence, counter,
                challengeSha256);

            log.Info("Signing bytes " + signedData.ToHexString());
            var signature = crypto.Sign(signedData, keyPair.PrivateKey);

            log.Info(" -- Outputs --");
            log.Info("  userPresence: " + userPresence);
            log.Info("  counter: " + counter);
            log.Info("  signature: " + signature.ToHexString());
            log.Info("<< authenticate");

            return new AuthenticateResponse(userPresence, counter, signature);
        }
    }
}