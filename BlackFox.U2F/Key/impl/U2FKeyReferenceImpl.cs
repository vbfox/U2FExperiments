using BlackFox.U2F.Codec;
using BlackFox.U2F.Key.messages;
using Common.Logging;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Key.impl
{
	public class U2FKeyReferenceImpl : IU2FKey
	{
        static readonly ILog log = LogManager.GetLogger(typeof(U2FKeyReferenceImpl));

		private readonly X509Certificate vendorCertificate;

		private readonly ECPrivateKeyParameters certificatePrivateKey;

		private readonly IKeyPairGenerator keyPairGenerator;

		private readonly IKeyHandleGenerator keyHandleGenerator;

		private readonly IDataStore dataStore;

		private readonly IUserPresenceVerifier userPresenceVerifier;

		private readonly ICrypto crypto;

		public U2FKeyReferenceImpl(X509Certificate vendorCertificate,
            ECPrivateKeyParameters certificatePrivateKey, IKeyPairGenerator
			 keyPairGenerator, IKeyHandleGenerator keyHandleGenerator, IDataStore
			 dataStore, IUserPresenceVerifier userPresenceVerifier, ICrypto
			 crypto)
		{
			this.vendorCertificate = vendorCertificate;
			this.certificatePrivateKey = certificatePrivateKey;
			this.keyPairGenerator = keyPairGenerator;
			this.keyHandleGenerator = keyHandleGenerator;
			this.dataStore = dataStore;
			this.userPresenceVerifier = userPresenceVerifier;
			this.crypto = crypto;
		}

		/// <exception cref="U2FException"/>
		public virtual RegisterResponse Register(RegisterRequest
			 registerRequest)
		{
			log.Info(">> register");
			var applicationSha256 = registerRequest.ApplicationSha256;
			var challengeSha256 = registerRequest.ChallengeSha256;
			log.Info(" -- Inputs --");
			log.Info("  applicationSha256: " + org.apache.commons.codec.binary.Hex.encodeHexString
				(applicationSha256));
			log.Info("  challengeSha256: " + org.apache.commons.codec.binary.Hex.encodeHexString
				(challengeSha256));
			var userPresent = userPresenceVerifier.VerifyUserPresence();
			if ((userPresent & UserPresenceVerifierConstants.UserPresentFlag) == 
				0)
			{
				throw new U2FException("Cannot verify user presence");
			}
			java.security.KeyPair keyPair = keyPairGenerator.GenerateKeyPair(applicationSha256
				, challengeSha256);
			var keyHandle = keyHandleGenerator.GenerateKeyHandle(applicationSha256, keyPair
				);
			dataStore.StoreKeyPair(keyHandle, keyPair);
			var userPublicKey = keyPairGenerator.EncodePublicKey(keyPair.getPublic());
			var signedData = RawMessageCodec.EncodeRegistrationSignedBytes
				(applicationSha256, challengeSha256, keyHandle, userPublicKey);
			log.Info("Signing bytes " + org.apache.commons.codec.binary.Hex.encodeHexString(signedData
				));
			var signature = crypto.Sign(signedData, certificatePrivateKey);
			log.Info(" -- Outputs --");
			log.Info("  userPublicKey: " + org.apache.commons.codec.binary.Hex.encodeHexString
				(userPublicKey));
			log.Info("  keyHandle: " + org.apache.commons.codec.binary.Hex.encodeHexString(keyHandle
				));
			log.Info("  vendorCertificate: " + vendorCertificate);
			log.Info("  signature: " + org.apache.commons.codec.binary.Hex.encodeHexString(signature
				));
			log.Info("<< register");
			return new RegisterResponse(userPublicKey, keyHandle, 
				vendorCertificate, signature);
		}

		/// <exception cref="U2FException"/>
		public virtual AuthenticateResponse Authenticate(AuthenticateRequest
			 authenticateRequest)
		{
			log.Info(">> authenticate");
			var control = authenticateRequest.Control;
			var applicationSha256 = authenticateRequest.ApplicationSha256;
			var challengeSha256 = authenticateRequest.ChallengeSha256;
			var keyHandle = authenticateRequest.KeyHandle;
			log.Info(" -- Inputs --");
			log.Info("  control: " + control);
			log.Info("  applicationSha256: " + org.apache.commons.codec.binary.Hex.encodeHexString
				(applicationSha256));
			log.Info("  challengeSha256: " + org.apache.commons.codec.binary.Hex.encodeHexString
				(challengeSha256));
			log.Info("  keyHandle: " + org.apache.commons.codec.binary.Hex.encodeHexString(keyHandle
				));
			java.security.KeyPair keyPair = dataStore.GetKeyPair(keyHandle);
			var counter = dataStore.IncrementCounter();
			var userPresence = userPresenceVerifier.VerifyUserPresence();
			var signedData = RawMessageCodec.EncodeAuthenticateSignedBytes
				(applicationSha256, userPresence, counter, challengeSha256);
			log.Info("Signing bytes " + org.apache.commons.codec.binary.Hex.encodeHexString(signedData
				));
			var signature = crypto.Sign(signedData, keyPair.getPrivate());
			log.Info(" -- Outputs --");
			log.Info("  userPresence: " + userPresence);
			log.Info("  counter: " + counter);
			log.Info("  signature: " + org.apache.commons.codec.binary.Hex.encodeHexString(signature
				));
			log.Info("<< authenticate");
			return new AuthenticateResponse(userPresence, counter
				, signature);
		}
	}
}
