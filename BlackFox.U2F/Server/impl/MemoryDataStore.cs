// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

using System;
using System.Collections.Generic;
using BlackFox.U2F.Server.data;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Server.impl
{
	public class MemoryDataStore : IDataStore
	{
	    readonly HashSet<X509Certificate> trustedCertificateDataBase = new HashSet<X509Certificate>();
	    readonly Dictionary<string, EnrollSessionData> sessionDataBase = new Dictionary<string, EnrollSessionData>();
        readonly Dictionary<string, IList<SecurityKeyData>> securityKeyDataBase = new Dictionary<string, IList<SecurityKeyData>>();

		private readonly ISessionIdGenerator sessionIdGenerator;

		public MemoryDataStore(ISessionIdGenerator sessionIdGenerator)
		{
		    if (sessionIdGenerator == null)
		    {
		        throw new ArgumentNullException(nameof(sessionIdGenerator));
		    }
		    this.sessionIdGenerator = sessionIdGenerator;
		}

	    public string StoreSessionData(EnrollSessionData sessionData)
		{
			var sessionId = sessionIdGenerator.GenerateSessionId(sessionData.GetAccountName());
			sessionDataBase[sessionId] = sessionData;
			return sessionId;
		}

		public EnrollSessionData GetEnrollSessionData(string sessionId)
		{
			return sessionDataBase[sessionId];
		}

		public SignSessionData GetSignSessionData(string sessionId)
		{
			return (SignSessionData)sessionDataBase[sessionId];
		}

		public void AddSecurityKeyData(string accountName, SecurityKeyData securityKeyData)
		{
			var tokens = GetSecurityKeyData(accountName);
			tokens.Add(securityKeyData);
			securityKeyDataBase[accountName] = tokens;
		}

		public IList<SecurityKeyData
			> GetSecurityKeyData(string accountName)
		{
			return com.google.common.@base.Objects.firstNonNull(securityKeyDataBase[accountName
				], com.google.common.collect.Lists.newArrayList<SecurityKeyData
				>());
		}

		public HashSet<Org.BouncyCastle.X509.X509Certificate> GetTrustedCertificates()
		{
			return trustedCertificateDataBase;
		}

		public void AddTrustedCertificate(Org.BouncyCastle.X509.X509Certificate certificate
			)
		{
			trustedCertificateDataBase.add(certificate);
		}

		public void RemoveSecuityKey(string accountName, byte[] publicKey)
		{
			var tokens
				 = GetSecurityKeyData(accountName);
			foreach (var token in tokens)
			{
				if (java.util.Arrays.equals(token.PublicKey, publicKey))
				{
					tokens.remove(token);
					break;
				}
			}
		}

		public void UpdateSecurityKeyCounter(string accountName, byte[] publicKey
			, int newCounterValue)
		{
			var tokens
				 = GetSecurityKeyData(accountName);
			foreach (var token in tokens)
			{
				if (java.util.Arrays.equals(token.PublicKey, publicKey))
				{
					token.Counter = newCounterValue;
					break;
				}
			}
		}
	}
}
