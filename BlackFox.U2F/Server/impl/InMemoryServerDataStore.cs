using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
                    var transports = keyData.Transports?.Select(t => (object) t.ToString()).ToArray();

                    var keyDataJson = new JObject
                    {
                        ["enrollmentTime"] = keyData.EnrollmentTime,
                        ["keyHandle"] = WebSafeBase64Converter.ToBase64String(keyData.KeyHandle),
                        ["publicKey"] = WebSafeBase64Converter.ToBase64String(keyData.PublicKey),
                        ["attestationCert"] = WebSafeBase64Converter.ToBase64String(attestationCert),
                        ["counter"] = keyData.Counter,
                        ["transports"] = transports != null ? new JArray(transports) : null
                    };
                    
                    keyDatas.Add(keyDataJson);
                }
                keys[key.Key] = keyDatas;
            }
            result["keys"] = keys;

            return result;
        }

        private void LoadFromJson(JObject json)
        {
            securityKeyDataBase.Clear();

            var keys = json.GetValue("keys") as JObject ?? new JObject();
            foreach (var pair in keys)
            {
                var accountName = pair.Key;
                var keyDatas = pair.Value;

                foreach (var keyData in keyDatas)
                {
                    var enrollmentTime = (long) keyData["enrollmentTime"];
                    var keyHandle = WebSafeBase64Converter.FromBase64String((string) keyData["keyHandle"]);
                    var publicKey = WebSafeBase64Converter.FromBase64String((string) keyData["publicKey"]);
                    var attestationCertBytes =
                        WebSafeBase64Converter.FromBase64String((string) keyData["attestationCert"]);
                    var attestationCert = new X509CertificateParser().ReadCertificate(attestationCertBytes);
                    var counter = (int) keyData["counter"];
                    var transportToken = keyData["transports"];
                    List<SecurityKeyDataTransports> transports = null;
                    if (transportToken != null && transportToken.Type != JTokenType.Null)
                    {
                        var transportsArray = (JArray) transportToken;
                        transports = transportsArray
                            .Select(o => (string) o)
                            .Select(
                                s => (SecurityKeyDataTransports) Enum.Parse(typeof (SecurityKeyDataTransports), s, true))
                            .ToList();
                    }
                    var securityKeyData = new SecurityKeyData(enrollmentTime, transports, keyHandle, publicKey,
                        attestationCert, counter);
                    AddSecurityKeyData(accountName, securityKeyData);
                }
            }
        }

        public void SaveToStream([NotNull] Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var textWriter = new StreamWriter(stream))
            using (var writer = new JsonTextWriter(textWriter))
            {
                writer.Formatting = Formatting.Indented;
                SaveToJson().WriteTo(writer);
            }
        }

        public void LoadFromStream([NotNull] Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var textReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(textReader))
            {
                var json = JObject.Load(reader);
                LoadFromJson(json);
            }
        }
    }
}