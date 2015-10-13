using BlackFox.BinaryUtils;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.impl;
using BlackFox.U2F.Key.messages;
using BlackFox.U2F.Server.data;
using BlackFox.U2F.Server.messages;
using Common.Logging;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Security.Certificates;

namespace BlackFox.U2F.Server.impl
{
	public class Iu2FServerReferenceImpl : IU2FServer
	{
		private const string TRANSPORT_EXTENSION_OID = "1.3.6.1.4.1.45724.2.1.1";

		private const int BITS_IN_A_BYTE = 8;

		private const string TYPE_PARAM = "typ";

		private const string CHALLENGE_PARAM = "challenge";

		private const string ORIGIN_PARAM = "origin";

		private const string CHANNEL_ID_PARAM = "cid_pubkey";

		private const string UNUSED_CHANNEL_ID = "";

        static readonly ILog log = LogManager.GetLogger(typeof(Iu2FServerReferenceImpl));

        private readonly IChallengeGenerator challengeGenerator;

		private readonly IDataStore dataStore;

		private readonly ICrypto cryto;

		private readonly System.Collections.Generic.ICollection<string> allowedOrigins;

		public Iu2FServerReferenceImpl(IChallengeGenerator challengeGenerator
			, IDataStore dataStore, ICrypto cryto, 
			System.Collections.Generic.ICollection<string> origins)
		{
			// Object Identifier for the attestation certificate transport extension fidoU2FTransports
			// The number of bits in a byte. It is used to know at which index in a BitSet to look for
			// specific transport values
			// TODO: use these for channel id checks in verifyBrowserData
			this.challengeGenerator = challengeGenerator;
			this.dataStore = dataStore;
			this.cryto = cryto;
			allowedOrigins = CanonicalizeOrigins(origins);
		}

		public RegistrationRequest GetRegistrationRequest
			(string accountName, string appId)
		{
			log.Info(">> getRegistrationRequest " + accountName);
			byte[] challenge = challengeGenerator.GenerateChallenge(accountName);
			EnrollSessionData sessionData = new EnrollSessionData
				(accountName, appId, challenge);
			string sessionId = dataStore.StoreSessionData(sessionData);
			string challengeBase64 = WebSafeBase64Converter.ToBase64String
				(challenge);
			log.Info("-- Output --");
			log.Info("  sessionId: " + sessionId);
			log.Info("  challenge: " + challenge.ToHexString());
			log.Info("<< getRegistrationRequest " + accountName);
			return new RegistrationRequest(U2FConsts
				.U2Fv2, challengeBase64, appId, sessionId);
		}

		/// <exception cref="U2FException"/>
		public SecurityKeyData ProcessRegistrationResponse(RegistrationResponse registrationResponse, long currentTimeInMillis)
		{
			log.Info(">> processRegistrationResponse");
			string sessionId = registrationResponse.GetSessionId();
			string browserDataBase64 = registrationResponse.GetBd();
			string rawRegistrationDataBase64 = registrationResponse.GetRegistrationData();
			EnrollSessionData sessionData = dataStore.GetEnrollSessionData
				(sessionId);
			if (sessionData == null)
			{
				throw new U2FException("Unknown session_id");
			}
			string appId = sessionData.GetAppId();
			string browserData = Sharpen.Runtime.getStringForBytes(org.apache.commons.codec.binary.Base64
				.decodeBase64(browserDataBase64));
			byte[] rawRegistrationData =WebSafeBase64Converter.FromBase64String(
				rawRegistrationDataBase64);
			log.Info("-- Input --");
			log.Info("  sessionId: " + sessionId);
			log.Info("  challenge: " + sessionData
			    .GetChallenge().ToHexString());
			log.Info("  accountName: " + sessionData.GetAccountName());
			log.Info("  browserData: " + browserData);
			log.Info("  rawRegistrationData: " + rawRegistrationData.ToHexString
				());
			RegisterResponse registerResponse = RawMessageCodec
				.DecodeRegisterResponse(rawRegistrationData);
			byte[] userPublicKey = registerResponse.UserPublicKey;
			byte[] keyHandle = registerResponse.KeyHandle;
			Org.BouncyCastle.X509.X509Certificate attestationCertificate = registerResponse.AttestationCertificate;
			byte[] signature = registerResponse.Signature;
			System.Collections.Generic.IList<SecurityKeyDataTransports
				> transports = null;
			try
			{
				transports = ParseTransportsExtension(attestationCertificate);
			}
			catch (java.security.cert.CertificateParsingException e1)
			{
				log.Warn("Could not parse transports extension " + e1.Message);
			}
			log.Info("-- Parsed rawRegistrationResponse --");
			log.Info("  userPublicKey: " + userPublicKey.ToHexString
				());
			log.Info("  keyHandle: " + keyHandle.ToHexString());
			log.Info("  attestationCertificate: " + attestationCertificate.ToString());
			log.Info("  transports: " + transports);
			try
			{
				log.Info("  attestationCertificate bytes: " + org.apache.commons.codec.binary.Hex
					.encodeHexString(attestationCertificate.getEncoded()));
			}
			catch (java.security.cert.CertificateEncodingException e)
			{
				throw new U2FException("Cannot encode certificate", e);
			}
			log.Info("  signature: " + signature.ToHexString());
			byte[] appIdSha256 = cryto.ComputeSha256(System.Text.Encoding.UTF8.GetBytes(appId)
				);
			byte[] browserDataSha256 = cryto.ComputeSha256(System.Text.Encoding.UTF8.GetBytes(
				browserData));
			byte[] signedBytes = RawMessageCodec.EncodeRegistrationSignedBytes
				(appIdSha256, browserDataSha256, keyHandle, userPublicKey);
			System.Collections.Generic.ICollection<Org.BouncyCastle.X509.X509Certificate> trustedCertificates
				 = dataStore.GetTrustedCertificates();
			if (!trustedCertificates.contains(attestationCertificate))
			{
				log.Warn("Attestion cert is not trusted");
			}
			VerifyBrowserData(new com.google.gson.JsonParser().parse(browserData), "navigator.id.finishEnrollment"
				, sessionData);
			log.Info("Verifying signature of bytes " + signedBytes.ToHexString
				());
			if (!cryto.verifySignature(attestationCertificate, signedBytes, signature))
			{
				throw new U2FException("Signature is invalid");
			}
			// The first time we create the SecurityKeyData, we set the counter value to 0.
			// We don't actually know what the counter value of the real device is - but it will
			// be something bigger (or equal) to 0, so subsequent signatures will check out ok.
			SecurityKeyData securityKeyData = new SecurityKeyData
				(currentTimeInMillis, transports, keyHandle, userPublicKey, attestationCertificate
				, 0);
			/* initial counter value */
			dataStore.AddSecurityKeyData(sessionData.GetAccountName(), securityKeyData);
			log.Info("<< processRegistrationResponse");
			return securityKeyData;
		}

		/// <exception cref="U2FException"/>
		public System.Collections.Generic.IList<SignRequest
			> GetSignRequest(string accountName, string appId)
		{
			log.Info(">> getSignRequest " + accountName);
			System.Collections.Generic.IList<SecurityKeyData> securityKeyDataList
				 = dataStore.GetSecurityKeyData(accountName);
			com.google.common.collect.ImmutableList.Builder<SignRequest
				> result = com.google.common.collect.ImmutableList.builder();
			foreach (SecurityKeyData securityKeyData in securityKeyDataList)
			{
				byte[] challenge = challengeGenerator.GenerateChallenge(accountName);
				SignSessionData sessionData = new SignSessionData
					(accountName, appId, challenge, securityKeyData.PublicKey);
				string sessionId = dataStore.StoreSessionData(sessionData);
				byte[] keyHandle = securityKeyData.KeyHandle;
				log.Info("-- Output --");
				log.Info("  sessionId: " + sessionId);
				log.Info("  challenge: " + challenge.ToHexString());
				log.Info("  keyHandle: " + keyHandle.ToHexString());
				string challengeBase64 = WebSafeBase64Converter.ToBase64String
					(challenge);
				string keyHandleBase64 = WebSafeBase64Converter.ToBase64String
					(keyHandle);
				log.Info("<< getSignRequest " + accountName);
				result.add(new SignRequest(U2FConsts
					.U2Fv2, challengeBase64, appId, keyHandleBase64, sessionId));
			}
			return ((com.google.common.collect.ImmutableList<SignRequest
				>)result.build());
		}

		/// <exception cref="U2FException"/>
		public SecurityKeyData ProcessSignResponse(SignResponse
			 signResponse)
		{
			log.Info(">> processSignResponse");
			string sessionId = signResponse.SessionId;
			string browserDataBase64 = signResponse.Bd;
			string rawSignDataBase64 = signResponse.Sign;
			SignSessionData sessionData = dataStore.GetSignSessionData
				(sessionId);
			if (sessionData == null)
			{
				throw new U2FException("Unknown session_id");
			}
			string appId = sessionData.GetAppId();
			SecurityKeyData securityKeyData = null;
			foreach (SecurityKeyData temp in dataStore.GetSecurityKeyData
				(sessionData.GetAccountName()))
			{
				if (java.util.Arrays.equals(sessionData.GetPublicKey(), temp.PublicKey))
				{
					securityKeyData = temp;
					break;
				}
			}
			if (securityKeyData == null)
			{
				throw new U2FException("No security keys registered for this user"
					);
			}
			string browserData = Sharpen.Runtime.getStringForBytes(org.apache.commons.codec.binary.Base64
				.decodeBase64(browserDataBase64));
			byte[] rawSignData =WebSafeBase64Converter.FromBase64String(rawSignDataBase64
				);
			log.Info("-- Input --");
			log.Info("  sessionId: " + sessionId);
			log.Info("  publicKey: " + securityKeyData.PublicKey.ToHexString());
			log.Info("  challenge: " + sessionData
			    .GetChallenge().ToHexString());
			log.Info("  accountName: " + sessionData.GetAccountName());
			log.Info("  browserData: " + browserData);
			log.Info("  rawSignData: " + rawSignData.ToHexString());
			VerifyBrowserData(new com.google.gson.JsonParser().parse(browserData), "navigator.id.getAssertion"
				, sessionData);
			AuthenticateResponse authenticateResponse = RawMessageCodec
				.DecodeAuthenticateResponse(rawSignData);
			byte userPresence = authenticateResponse.UserPresence;
			int counter = authenticateResponse.Counter;
			byte[] signature = authenticateResponse.Signature;
			log.Info("-- Parsed rawSignData --");
			log.Info("  userPresence: " + int.toHexString(userPresence & unchecked((int)(0xFF
				))));
			log.Info("  counter: " + counter);
			log.Info("  signature: " + signature.ToHexString());
			if (userPresence != UserPresenceVerifierConstants.UserPresentFlag)
			{
				throw new U2FException("User presence invalid during authentication"
					);
			}
			if (counter <= securityKeyData.Counter)
			{
				throw new U2FException("Counter value smaller than expected!");
			}
			byte[] appIdSha256 = cryto.ComputeSha256(System.Text.Encoding.UTF8.GetBytes(appId)
				);
			byte[] browserDataSha256 = cryto.ComputeSha256(System.Text.Encoding.UTF8.GetBytes(
				browserData));
			byte[] signedBytes = RawMessageCodec.EncodeAuthenticateSignedBytes
				(appIdSha256, userPresence, counter, browserDataSha256);
			log.Info("Verifying signature of bytes " + signedBytes.ToHexString
				());
			if (!cryto.verifySignature(cryto.DecodePublicKey(securityKeyData.PublicKey), 
				signedBytes, signature))
			{
				throw new U2FException("Signature is invalid");
			}
			dataStore.UpdateSecurityKeyCounter(sessionData.GetAccountName(), securityKeyData.PublicKey, counter);
			log.Info("<< processSignResponse");
			return securityKeyData;
		}

        /// <summary>
        /// Parses a transport extension from an attestation certificate and returns
        /// a List of HardwareFeatures supported by the security key.
        /// </summary>
        /// <remarks>
        /// Parses a transport extension from an attestation certificate and returns
        /// a List of HardwareFeatures supported by the security key. The specification of
        /// the HardwareFeatures in the certificate should match their internal definition in
        /// device_auth.proto
        /// <p>The expected transport extension value is a BIT STRING containing the enabled
        /// transports:</p>
        /// <p>FIDOU2FTransports ::= BIT STRING {
        /// bluetoothRadio(0), -- Bluetooth Classic
        /// bluetoothLowEnergyRadio(1),
        /// uSB(2),
        /// nFC(3)
        /// }</p>
        /// <p>Note that the BIT STRING must be wrapped in an OCTET STRING.
        /// An extension that encodes BT, BLE, and NFC then looks as follows:</p>
        /// <p>SEQUENCE (2 elem)
        /// OBJECT IDENTIFIER 1.3.6.1.4.1.45724.2.1.1
        /// OCTET STRING (1 elem)
        /// BIT STRING (4 bits) 1101</p>
        /// </remarks>
        /// <param name="cert">the certificate to parse for extension</param>
        /// <returns>
        /// the supported transports as a List of HardwareFeatures or null if no extension
        /// was found
        /// </returns>
        /// <exception cref="CertificateParsingException"/>
        public static System.Collections.Generic.IList<SecurityKeyDataTransports
			> ParseTransportsExtension(Org.BouncyCastle.X509.X509Certificate cert)
		{
			byte[] extValue = cert.getExtensionValue(TRANSPORT_EXTENSION_OID);
			System.Collections.Generic.LinkedList<SecurityKeyDataTransports
				> transportsList = new System.Collections.Generic.LinkedList<SecurityKeyDataTransports
				>();
			if (extValue == null)
			{
				// No transports extension found.
				return null;
			}
			var ais = new Asn1InputStream
				(extValue);
			Asn1Object asn1Object;
			// Read out the OctetString
			try
			{
				asn1Object = ais.ReadObject();
				ais.Dispose();
			}
			catch (System.IO.IOException e)
			{
				throw new java.security.cert.CertificateParsingException("Not able to read object in transports extenion"
					, e);
			}
			if (asn1Object == null || !(asn1Object is org.bouncycastle.asn1.DEROctetString))
			{
				throw new java.security.cert.CertificateParsingException("No Octet String found in transports extension"
					);
			}
			org.bouncycastle.asn1.DEROctetString octet = (org.bouncycastle.asn1.DEROctetString
				)asn1Object;
			// Read out the BitString
			ais = new org.bouncycastle.asn1.ASN1InputStream(octet.getOctets());
			try
			{
				asn1Object = ais.readObject();
				ais.close();
			}
			catch (System.IO.IOException e)
			{
				throw new java.security.cert.CertificateParsingException("Not able to read object in transports extension"
					, e);
			}
			if (asn1Object == null || !(asn1Object is org.bouncycastle.asn1.DERBitString))
			{
				throw new java.security.cert.CertificateParsingException("No BitString found in transports extension"
					);
			}
			org.bouncycastle.asn1.DERBitString bitString = (org.bouncycastle.asn1.DERBitString
				)asn1Object;
			byte[] values = bitString.getBytes();
			java.util.BitSet bitSet = java.util.BitSet.valueOf(values);
			// We might have more defined transports than used by the extension
			for (int i = 0; i < BITS_IN_A_BYTE; i++)
			{
				if (bitSet.get(BITS_IN_A_BYTE - i - 1))
				{
					transportsList.add(SecurityKeyDataTransports.values()
						[i]);
				}
			}
			return transportsList;
		}

		/// <exception cref="U2FException"/>
		private void VerifyBrowserData(com.google.gson.JsonElement browserDataAsElement, 
			string messageType, EnrollSessionData sessionData)
		{
			if (!browserDataAsElement.isJsonObject())
			{
				throw new U2FException("browserdata has wrong format");
			}
			JObject browserData = browserDataAsElement.getAsJsonObject();
			// check that the right "typ" parameter is present in the browserdata JSON
			if (!browserData.has(TYPE_PARAM))
			{
				throw new U2FException("bad browserdata: missing 'typ' param");
			}
			string type = browserData.get(TYPE_PARAM).getAsString();
			if (!messageType.Equals(type))
			{
				throw new U2FException("bad browserdata: bad type " + type);
			}
			// check that the right challenge is in the browserdata
			if (!browserData.has(CHALLENGE_PARAM))
			{
				throw new U2FException("bad browserdata: missing 'challenge' param"
					);
			}
			if (browserData.has(ORIGIN_PARAM))
			{
				VerifyOrigin(browserData.get(ORIGIN_PARAM).getAsString());
			}
			byte[] challengeFromBrowserData =WebSafeBase64Converter.FromBase64String
				(browserData.get(CHALLENGE_PARAM).getAsString());
			if (!java.util.Arrays.equals(challengeFromBrowserData, sessionData.GetChallenge()
				))
			{
				throw new U2FException("wrong challenge signed in browserdata");
			}
		}

		// TODO: Deal with ChannelID
		/// <exception cref="U2FException"/>
		private void VerifyOrigin(string origin)
		{
			if (!allowedOrigins.contains(CanonicalizeOrigin(origin)))
			{
				throw new U2FException(origin + " is not a recognized home origin for this backend"
					);
			}
		}

		public System.Collections.Generic.IList<SecurityKeyData
			> GetAllSecurityKeys(string accountName)
		{
			return dataStore.GetSecurityKeyData(accountName);
		}

		/// <exception cref="U2FException"/>
		public void RemoveSecurityKey(string accountName, byte[] publicKey)
		{
			dataStore.RemoveSecuityKey(accountName, publicKey);
		}

		private static System.Collections.Generic.ICollection<string> CanonicalizeOrigins
			(System.Collections.Generic.ICollection<string> origins)
		{
			com.google.common.collect.ImmutableSet.Builder<string> result = com.google.common.collect.ImmutableSet
				.builder();
			foreach (string origin in origins)
			{
				result.add(CanonicalizeOrigin(origin));
			}
			return ((com.google.common.collect.ImmutableSet<string>)result.build());
		}

		internal static string CanonicalizeOrigin(string url)
		{
			java.net.URI uri;
			try
			{
				uri = new java.net.URI(url);
			}
			catch (java.net.URISyntaxException e)
			{
				throw new System.Exception("specified bad origin", e);
			}
			return uri.getScheme() + "://" + uri.getAuthority();
		}
	}
}
