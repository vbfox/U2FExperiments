using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BlackFox.Binary;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using BlackFox.U2F.Server.data;
using BlackFox.U2F.Server.messages;
using Common.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Server.impl
{
    public class U2FServerReferenceImpl : IU2FServer
    {
        private const int BITS_IN_A_BYTE = 8;
        private const string TYPE_PARAM = "typ";
        private const string CHALLENGE_PARAM = "challenge";
        private const string ORIGIN_PARAM = "origin";
        private const string CHANNEL_ID_PARAM = "cid_pubkey";
        private const string UNUSED_CHANNEL_ID = "";

        private static readonly DerObjectIdentifier transportExtensionOid =
            new DerObjectIdentifier("1.3.6.1.4.1.45724.2.1.1");

        private static readonly ILog log = LogManager.GetLogger(typeof (U2FServerReferenceImpl));
        private readonly ICollection<string> allowedOrigins;

        private readonly IChallengeGenerator challengeGenerator;
        private readonly IServerCrypto cryto;
        private readonly IServerDataStore dataStore;

        public U2FServerReferenceImpl(IChallengeGenerator challengeGenerator, IServerDataStore dataStore, IServerCrypto cryto,
            ICollection<string> origins)
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

        public RegistrationRequest GetRegistrationRequest(string accountName, string appId)
        {
            log.Info(">> getRegistrationRequest " + accountName);

            var challenge = challengeGenerator.GenerateChallenge(accountName);
            var sessionData = new EnrollSessionData(accountName, appId, challenge);
            var sessionId = dataStore.StoreSessionData(sessionData);
            var challengeBase64 = WebSafeBase64Converter.ToBase64String(challenge);

            log.Info("-- Output --");
            log.Info("  sessionId: " + sessionId);
            log.Info("  challenge: " + challenge.ToHexString());
            log.Info("<< getRegistrationRequest " + accountName);

            return new RegistrationRequest(U2FConsts.U2Fv2, challengeBase64, appId, sessionId);
        }

        /// <exception cref="U2FException" />
        public SecurityKeyData ProcessRegistrationResponse(RegistrationResponse registrationResponse,
            long currentTimeInMillis)
        {
            log.Info(">> processRegistrationResponse");

            var sessionId = registrationResponse.SessionId;
            var browserDataBase64 = registrationResponse.Bd;
            var rawRegistrationDataBase64 = registrationResponse.RegistrationData;
            var sessionData = dataStore.GetEnrollSessionData(sessionId);
            if (sessionData == null)
            {
                throw new U2FException("Unknown session_id");
            }
            var appId = sessionData.AppId;
            var rawBrowserData = WebSafeBase64Converter.FromBase64String(browserDataBase64);
            var browserData = Encoding.UTF8.GetString(rawBrowserData, 0, rawBrowserData.Length);
            var rawRegistrationData = WebSafeBase64Converter.FromBase64String(rawRegistrationDataBase64);

            log.Info("-- Input --");
            log.Info("  sessionId: " + sessionId);
            log.Info("  challenge: " + sessionData.Challenge.ToHexString());
            log.Info("  accountName: " + sessionData.AccountName);
            log.Info("  browserData: " + browserData);
            log.Info("  rawRegistrationData: " + rawRegistrationData.ToHexString());

            var registerResponse = RawMessageCodec.DecodeRegisterResponse(rawRegistrationData.Segment());
            var userPublicKey = registerResponse.UserPublicKey;
            var keyHandle = registerResponse.KeyHandle;
            var attestationCertificate = registerResponse.AttestationCertificate;
            var signature = registerResponse.Signature;
            IList<SecurityKeyDataTransports> transports = null;
            try
            {
                transports = ParseTransportsExtension(attestationCertificate);
            }
            catch (CertificateParsingException e1)
            {
                log.Warn("Could not parse transports extension " + e1.Message);
            }
            log.Info("-- Parsed rawRegistrationResponse --");
            log.Info("  userPublicKey: " + userPublicKey.ToHexString
                ());
            log.Info("  keyHandle: " + keyHandle.ToHexString());
            log.Info("  attestationCertificate: " + SecurityKeyData.SafeCertificateToString(attestationCertificate));
            log.Info("  transports: " + transports);
            log.Info("  attestationCertificate bytes: " + attestationCertificate.GetEncoded().ToHexString());
            log.Info("  signature: " + signature.ToHexString());

            var appIdSha256 = cryto.ComputeSha256(Encoding.UTF8.GetBytes(appId));
            var browserDataSha256 = cryto.ComputeSha256(Encoding.UTF8.GetBytes(browserData));
            var signedBytes = RawMessageCodec.EncodeRegistrationSignedBytes(appIdSha256, browserDataSha256, keyHandle,
                userPublicKey);
            var trustedCertificates = dataStore.GetTrustedCertificates();
            if (!trustedCertificates.Contains(attestationCertificate))
            {
                log.Warn("Attestion cert is not trusted");
            }

            VerifyBrowserData(browserData, "navigator.id.finishEnrollment", sessionData);

            log.Info("Verifying signature of bytes " + signedBytes.ToHexString());
            if (!cryto.VerifySignature(attestationCertificate, signedBytes, signature))
            {
                throw new U2FException("Signature is invalid");
            }

            // The first time we create the SecurityKeyData, we set the counter value to -1.
            // We don't actually know what the counter value of the real device is - but it will
            // be something bigger than -1, so subsequent signatures will check out ok.
            var securityKeyData = new SecurityKeyData(currentTimeInMillis, transports, keyHandle, userPublicKey,
                attestationCertificate, -1);

            /* initial counter value */
            dataStore.AddSecurityKeyData(sessionData.AccountName, securityKeyData);
            log.Info("<< processRegistrationResponse");
            return securityKeyData;
        }

        public IList<SignRequest> GetSignRequests(string accountName, string appId)
        {
            log.Info(">> getSignRequest " + accountName);
            var securityKeyDataList = dataStore.GetSecurityKeyData(accountName);
            var result = new List<SignRequest>();
            foreach (var securityKeyData in securityKeyDataList)
            {
                var challenge = challengeGenerator.GenerateChallenge(accountName);
                var sessionData = new SignSessionData(accountName, appId, challenge, securityKeyData.PublicKey);
                var sessionId = dataStore.StoreSessionData(sessionData);
                var keyHandle = securityKeyData.KeyHandle;
                log.Info("-- Output --");
                log.Info("  sessionId: " + sessionId);
                log.Info("  challenge: " + challenge.ToHexString());
                log.Info("  keyHandle: " + keyHandle.ToHexString());
                var challengeBase64 = WebSafeBase64Converter.ToBase64String(challenge);
                var keyHandleBase64 = WebSafeBase64Converter.ToBase64String(keyHandle);
                log.Info("<< getSignRequest " + accountName);
                result.Add(new SignRequest(U2FConsts.U2Fv2, challengeBase64, appId, keyHandleBase64, sessionId));
            }
            return result;
        }

        public SecurityKeyData ProcessSignResponse(SignResponse signResponse)
        {
            log.Info(">> processSignResponse");
            var sessionId = signResponse.SessionId;
            var browserDataBase64 = signResponse.Bd;
            var rawSignDataBase64 = signResponse.Sign;
            var sessionData = dataStore.GetSignSessionData(sessionId);
            if (sessionData == null)
            {
                throw new U2FException("Unknown session_id");
            }

            var appId = sessionData.AppId;
            var securityKeyData = dataStore
                .GetSecurityKeyData(sessionData.AccountName)
                .FirstOrDefault(k => sessionData.GetPublicKey().SequenceEqual(k.PublicKey));
            if (securityKeyData == null)
            {
                throw new U2FException("No security keys registered for this user");
            }

            var browserDataBytes = WebSafeBase64Converter.FromBase64String(browserDataBase64);
            var browserData = Encoding.UTF8.GetString(browserDataBytes, 0, browserDataBytes.Length);
            var rawSignData = WebSafeBase64Converter.FromBase64String(rawSignDataBase64);

            log.Info("-- Input --");
            log.Info("  sessionId: " + sessionId);
            log.Info("  publicKey: " + securityKeyData.PublicKey.ToHexString());
            log.Info("  challenge: " + sessionData.Challenge.ToHexString());
            log.Info("  accountName: " + sessionData.AccountName);
            log.Info("  browserData: " + browserData);
            log.Info("  rawSignData: " + rawSignData.ToHexString());

            VerifyBrowserData(browserData, "navigator.id.getAssertion", sessionData);
            var authenticateResponse = RawMessageCodec.DecodeAuthenticateResponse(rawSignData.Segment());
            var userPresence = authenticateResponse.UserPresence;
            var counter = authenticateResponse.Counter;
            var signature = authenticateResponse.Signature;

            log.Info("-- Parsed rawSignData --");
            log.Info("  userPresence: " + userPresence.ToString("X2"));
            log.Info("  counter: " + counter);
            log.Info("  signature: " + signature.ToHexString());

            if (userPresence != UserPresenceVerifierConstants.UserPresentFlag)
            {
                throw new U2FException("User presence invalid during authentication");
            }
            if (counter <= securityKeyData.Counter)
            {
                throw new U2FException("Counter value smaller than expected!");
            }

            var appIdSha256 = cryto.ComputeSha256(Encoding.UTF8.GetBytes(appId));
            var browserDataSha256 = cryto.ComputeSha256(Encoding.UTF8.GetBytes(browserData));
            var signedBytes = RawMessageCodec.EncodeAuthenticateSignedBytes(appIdSha256, userPresence, counter,
                browserDataSha256);
            log.Info("Verifying signature of bytes " + signedBytes.ToHexString());
            if (!cryto.VerifySignature(cryto.DecodePublicKey(securityKeyData.PublicKey), signedBytes, signature))
            {
                throw new U2FException("Signature is invalid");
            }
            dataStore.UpdateSecurityKeyCounter(sessionData.AccountName, securityKeyData.PublicKey, counter);
            log.Info("<< processSignResponse");
            return securityKeyData;
        }

        public IList<SecurityKeyData
            > GetAllSecurityKeys(string accountName)
        {
            return dataStore.GetSecurityKeyData(accountName);
        }

        /// <exception cref="U2FException" />
        public void RemoveSecurityKey(string accountName, byte[] publicKey)
        {
            dataStore.RemoveSecuityKey(accountName, publicKey);
        }

        /// <summary>
        ///     Parses a transport extension from an attestation certificate and returns
        ///     a List of HardwareFeatures supported by the security key.
        /// </summary>
        /// <remarks>
        ///     Parses a transport extension from an attestation certificate and returns
        ///     a List of HardwareFeatures supported by the security key. The specification of
        ///     the HardwareFeatures in the certificate should match their internal definition in
        ///     device_auth.proto
        ///     <p>
        ///         The expected transport extension value is a BIT STRING containing the enabled
        ///         transports:
        ///     </p>
        ///     <p>
        ///         FIDOU2FTransports ::= BIT STRING {
        ///         bluetoothRadio(0), -- Bluetooth Classic
        ///         bluetoothLowEnergyRadio(1),
        ///         uSB(2),
        ///         nFC(3)
        ///         }
        ///     </p>
        ///     <p>
        ///         Note that the BIT STRING must be wrapped in an OCTET STRING.
        ///         An extension that encodes BT, BLE, and NFC then looks as follows:
        ///     </p>
        ///     <p>
        ///         SEQUENCE (2 elem)
        ///         OBJECT IDENTIFIER 1.3.6.1.4.1.45724.2.1.1
        ///         OCTET STRING (1 elem)
        ///         BIT STRING (4 bits) 1101
        ///     </p>
        /// </remarks>
        /// <param name="cert">the certificate to parse for extension</param>
        /// <returns>
        ///     the supported transports as a List of HardwareFeatures or null if no extension
        ///     was found
        /// </returns>
        /// <exception cref="CertificateParsingException" />
        public static IList<SecurityKeyDataTransports> ParseTransportsExtension(X509Certificate cert)
        {
            var extValue = cert.GetExtensionValue(transportExtensionOid);
            var transportsList = new List<SecurityKeyDataTransports>();
            if (extValue == null)
            {
                // No transports extension found.
                return null;
            }

            // Read out the OctetString
            var asn1Object = extValue.ToAsn1Object();
            if (!(asn1Object is DerOctetString))
            {
                throw new CertificateParsingException("No Octet String found in transports extension");
            }
            var octet = (DerOctetString) asn1Object;

            // Read out the BitString

            try
            {
                using (var ais = new Asn1InputStream(octet.GetOctets()))
                {
                    asn1Object = ais.ReadObject();
                }
            }
            catch (IOException e)
            {
                throw new CertificateParsingException("Not able to read object in transports extension", e);
            }
            if (!(asn1Object is DerBitString))
            {
                throw new CertificateParsingException("No BitString found in transports extension");
            }

            var bitString = (DerBitString) asn1Object;
            var values = bitString.GetBytes();
            var bitSet = new BitArray(values);
            // We might have more defined transports than used by the extension
            for (var i = 0; i < BITS_IN_A_BYTE; i++)
            {
                if (bitSet.Get(BITS_IN_A_BYTE - i - 1))
                {
                    transportsList.Add((SecurityKeyDataTransports) i);
                }
            }
            return transportsList;
        }

        /// <exception cref="U2FException" />
        private void VerifyBrowserData(string browserData, string messageType, EnrollSessionData sessionData)
        {
            JObject browserDataObject;
            try
            {
                browserDataObject = JObject.Parse(browserData);
            }
            catch (JsonReaderException e)
            {
                throw new U2FException("browserdata has wrong format", e);
            }

            VerifyBrowserData(browserDataObject, messageType, sessionData);
        }

        /// <exception cref="U2FException" />
        private void VerifyBrowserData(JObject browserData, string messageType, EnrollSessionData sessionData)
        {
            // check that the right "typ" parameter is present in the browserdata JSON
            var typeProperty = browserData.Property(TYPE_PARAM);
            if (typeProperty == null)
            {
                throw new U2FException($"bad browserdata: missing '{TYPE_PARAM}' param");
            }
            var type = typeProperty.Value.ToString();
            if (messageType != type)
            {
                throw new U2FException("bad browserdata: bad type " + type);
            }

            var originProperty = browserData.Property(ORIGIN_PARAM);
            if (originProperty != null)
            {
                VerifyOrigin(originProperty.Value.ToString());
            }

            // check that the right challenge is in the browserdata
            var challengeProperty = browserData.Property(CHALLENGE_PARAM);
            if (challengeProperty == null)
            {
                throw new U2FException($"bad browserdata: missing '{CHALLENGE_PARAM}' param");
            }
            var challengeFromBrowserData = WebSafeBase64Converter.FromBase64String(challengeProperty.Value.ToString());
            if (!challengeFromBrowserData.SequenceEqual(sessionData.Challenge))
            {
                throw new U2FException("wrong challenge signed in browserdata");
            }
        }

        // TODO: Deal with ChannelID
        /// <exception cref="U2FException" />
        private void VerifyOrigin(string origin)
        {
            if (!allowedOrigins.Contains(CanonicalizeOrigin(origin)))
            {
                throw new U2FException(origin + " is not a recognized home origin for this backend");
            }
        }

        private static ICollection<string> CanonicalizeOrigins(IEnumerable<string> origins)
        {
            return origins.Select(CanonicalizeOrigin).ToList();
        }

        internal static string CanonicalizeOrigin(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                throw new U2FException($"Specified bad origin: {url}");
            }
            return uri.Scheme + "://" + GetAuthority(uri);
        }

        private static string GetAuthority(Uri uri)
        {
            var isDefaultPort =
                (uri.Scheme == "http" && uri.Port == 80) ||
                (uri.Scheme == "https" && uri.Port == 443);

            return uri.DnsSafeHost + (isDefaultPort ? "" : ":" + uri.Port);
        }
    }
}