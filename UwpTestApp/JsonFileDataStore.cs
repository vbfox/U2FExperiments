using BlackFox.U2F.Server;
using System.Collections.Generic;
using System.IO;
using BlackFox.U2F.Server.data;
using BlackFox.U2F.Server.impl;
using Org.BouncyCastle.X509;

namespace UwpTestApp
{
    public class JsonFileDataStore : IServerDataStore
    {
        private readonly string filePath;
        private readonly InMemoryServerDataStore inMemoryStore;

        public JsonFileDataStore(ISessionIdGenerator sessionIdGenerator, string filePath)
        {
            this.filePath = filePath;

            inMemoryStore = new InMemoryServerDataStore(sessionIdGenerator);

            Load();
        }

        public void AddTrustedCertificate(X509Certificate certificate)
        {
            inMemoryStore.AddTrustedCertificate(certificate);
        }

        public List<X509Certificate> GetTrustedCertificates()
        {
            return inMemoryStore.GetTrustedCertificates();
        }

        public string StoreSessionData(EnrollSessionData sessionData)
        {
            return inMemoryStore.StoreSessionData(sessionData);
        }

        public SignSessionData GetSignSessionData(string sessionId)
        {
            return inMemoryStore.GetSignSessionData(sessionId);
        }

        public EnrollSessionData GetEnrollSessionData(string sessionId)
        {
            return inMemoryStore.GetEnrollSessionData(sessionId);
        }

        public void AddSecurityKeyData(string accountName, SecurityKeyData securityKeyData)
        {
            inMemoryStore.AddSecurityKeyData(accountName, securityKeyData);
            Save();
        }

        private void Load()
        {
            if (File.Exists(filePath))
            {
                using (var stream = File.OpenRead(filePath))
                {
                    inMemoryStore.LoadFromStream(stream);
                }
            }
        }

        private void Save()
        {
            using (var stream = File.OpenWrite(filePath))
            {
                inMemoryStore.SaveToStream(stream);
            }
        }

        public IList<SecurityKeyData> GetSecurityKeyData(string accountName)
        {
            return inMemoryStore.GetSecurityKeyData(accountName);
        }

        public void RemoveSecurityKey(string accountName, byte[] publicKey)
        {
            inMemoryStore.RemoveSecurityKey(accountName, publicKey);
            Save();
        }

        public void UpdateSecurityKeyCounter(string accountName, byte[] publicKey, int newCounterValue)
        {
            inMemoryStore.UpdateSecurityKeyCounter(accountName, publicKey, newCounterValue);
            Save();
        }
    }
}
