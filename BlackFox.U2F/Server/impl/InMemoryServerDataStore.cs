using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BlackFox.U2F.Server.data;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Server.impl
{
    public class InMemoryServerDataStore : IServerDataStore
    {
        private readonly Dictionary<string, IList<SecurityKeyData>> securityKeyDataBase =
            new Dictionary<string, IList<SecurityKeyData>>();

        private readonly Dictionary<string, EnrollSessionData> sessionDataBase =
            new Dictionary<string, EnrollSessionData>();

        private readonly ISessionIdGenerator sessionIdGenerator;
        private readonly List<X509Certificate> trustedCertificateDataBase = new List<X509Certificate>();

        public InMemoryServerDataStore(ISessionIdGenerator sessionIdGenerator)
        {
            if (sessionIdGenerator == null)
            {
                throw new ArgumentNullException(nameof(sessionIdGenerator));
            }
            this.sessionIdGenerator = sessionIdGenerator;
        }

        public string StoreSessionData(EnrollSessionData sessionData)
        {
            var sessionId = sessionIdGenerator.GenerateSessionId(sessionData.AccountName);
            sessionDataBase[sessionId] = sessionData;
            return sessionId;
        }

        public EnrollSessionData GetEnrollSessionData(string sessionId)
        {
            return sessionDataBase[sessionId];
        }

        public SignSessionData GetSignSessionData(string sessionId)
        {
            return (SignSessionData) sessionDataBase[sessionId];
        }

        public void AddSecurityKeyData(string accountName, SecurityKeyData securityKeyData)
        {
            var tokens = GetSecurityKeyData(accountName);
            tokens.Add(securityKeyData);
            securityKeyDataBase[accountName] = tokens;
        }

        public IList<SecurityKeyData> GetSecurityKeyData(string accountName)
        {
            return securityKeyDataBase.ContainsKey(accountName)
                ? securityKeyDataBase[accountName]
                : new List<SecurityKeyData>();
        }

        public List<X509Certificate> GetTrustedCertificates()
        {
            return trustedCertificateDataBase;
        }

        public void AddTrustedCertificate(X509Certificate certificate)
        {
            trustedCertificateDataBase.Add(certificate);
        }

        public void RemoveSecuityKey(string accountName, byte[] publicKey)
        {
            var tokens = GetSecurityKeyData(accountName);
            var token = tokens.FirstOrDefault(t => t.PublicKey.SequenceEqual(publicKey));
            if (token != null)
            {
                tokens.Remove(token);
            }
        }

        public void UpdateSecurityKeyCounter(string accountName, byte[] publicKey, int newCounterValue)
        {
            var tokens = GetSecurityKeyData(accountName);
            var token = tokens.FirstOrDefault(t => t.PublicKey.SequenceEqual(publicKey));
            if (token != null)
            {
                token.Counter = newCounterValue;
            }
        }

        public JObject SaveToJson()
        {
            var result = new JObject();

            var keys = new JObject();
            foreach (var key in securityKeyDataBase)
            {
                var keyDatas = new JArray();
                foreach (var keyData in key.Value)
                {
                    var attestationCert = keyData.AttestationCertificate.GetEncoded();
                    var keyDataJson = new JObject
                    {
                        ["enrollmentTime"] = keyData.EnrollmentTime,
                        ["keyHandle"] = WebSafeBase64Converter.ToBase64String(keyData.KeyHandle),
                        ["publicKey"] = WebSafeBase64Converter.ToBase64String(keyData.PublicKey),
                        ["attestationCert"] = WebSafeBase64Converter.ToBase64String(attestationCert),
                        ["counter"] = keyData.Counter
                    };
                    keyDatas.Add(keyDataJson);
                }
                keys[key.Key] = keyDatas;
            }
            result["keys"] = keys;

            return result;
        }

        public void SaveToStream([NotNull] Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var writer = new JsonTextWriter(new StreamWriter(stream)))
            {
                writer.Formatting = Formatting.Indented;
                SaveToJson().WriteTo(writer);
            }
        }
    }
}