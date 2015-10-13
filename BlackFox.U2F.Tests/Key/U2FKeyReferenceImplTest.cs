// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

namespace BlackFox.U2F.Tests.Key
{/*
	public class U2FKeyReferenceImplTest : com.google.u2f.TestVectors
	{
		[org.mockito.Mock]
		internal com.google.u2f.key.KeyPairGenerator mockKeyPairGenerator;

		[org.mockito.Mock]
		internal com.google.u2f.key.KeyHandleGenerator mockKeyHandleGenerator;

		[org.mockito.Mock]
		internal com.google.u2f.key.DataStore mockDataStore;

		[org.mockito.Mock]
		internal com.google.u2f.key.UserPresenceVerifier mockUserPresenceVerifier;

		private com.google.u2f.key.U2FKey u2fKey;

		[NUnit.Framework.SetUp]
		public virtual void setup()
		{
			org.mockito.MockitoAnnotations.initMocks(this);
			u2fKey = new com.google.u2f.key.impl.U2FKeyReferenceImpl(VENDOR_CERTIFICATE, VENDOR_CERTIFICATE_PRIVATE_KEY
				, mockKeyPairGenerator, mockKeyHandleGenerator, mockDataStore, mockUserPresenceVerifier
				, new com.google.u2f.key.impl.BouncyCastleCrypto());
			org.mockito.Mockito.when(mockUserPresenceVerifier.verifyUserPresence()).thenReturn
				(com.google.u2f.key.UserPresenceVerifier.USER_PRESENT_FLAG);
			org.mockito.Mockito.when(mockKeyPairGenerator.generateKeyPair(APP_ID_ENROLL_SHA256
				, BROWSER_DATA_ENROLL_SHA256)).thenReturn(USER_KEY_PAIR_ENROLL);
			org.mockito.Mockito.when(mockKeyPairGenerator.encodePublicKey(USER_PUBLIC_KEY_ENROLL
				)).thenReturn(USER_PUBLIC_KEY_ENROLL_HEX);
			org.mockito.Mockito.when(mockKeyHandleGenerator.generateKeyHandle(APP_ID_ENROLL_SHA256
				, USER_KEY_PAIR_ENROLL)).thenReturn(KEY_HANDLE);
			org.mockito.Mockito.when(mockDataStore.getKeyPair(KEY_HANDLE)).thenReturn(USER_KEY_PAIR_SIGN
				);
			org.mockito.Mockito.when(mockDataStore.incrementCounter()).thenReturn(COUNTER_VALUE
				);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testRegister()
		{
			com.google.u2f.key.messages.RegisterRequest registerRequest = new com.google.u2f.key.messages.RegisterRequest
				(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256);
			com.google.u2f.key.messages.RegisterResponse registerResponse = u2fKey.register(registerRequest
				);
			org.mockito.Mockito.verify(mockDataStore).storeKeyPair(KEY_HANDLE, USER_KEY_PAIR_ENROLL
				);
			NUnit.Framework.Assert.assertArrayEquals(USER_PUBLIC_KEY_ENROLL_HEX, registerResponse
				.getUserPublicKey());
			NUnit.Framework.Assert.AreEqual(VENDOR_CERTIFICATE, registerResponse.getAttestationCertificate
				());
			NUnit.Framework.Assert.assertArrayEquals(KEY_HANDLE, registerResponse.getKeyHandle
				());
			java.security.Signature ecdsaSignature = java.security.Signature.getInstance("SHA256withECDSA"
				);
			ecdsaSignature.initVerify(VENDOR_CERTIFICATE.getPublicKey());
			ecdsaSignature.update(EXPECTED_REGISTER_SIGNED_BYTES);
			NUnit.Framework.Assert.IsTrue(ecdsaSignature.verify(registerResponse.getSignature
				()));
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testAuthenticate()
		{
			com.google.u2f.key.messages.AuthenticateRequest authenticateRequest = new com.google.u2f.key.messages.AuthenticateRequest
				(com.google.u2f.key.messages.AuthenticateRequest.USER_PRESENCE_SIGN, BROWSER_DATA_SIGN_SHA256
				, APP_ID_SIGN_SHA256, KEY_HANDLE);
			com.google.u2f.key.messages.AuthenticateResponse authenticateResponse = u2fKey.authenticate
				(authenticateRequest);
			NUnit.Framework.Assert.AreEqual(com.google.u2f.key.UserPresenceVerifier.USER_PRESENT_FLAG
				, authenticateResponse.getUserPresence());
			NUnit.Framework.Assert.AreEqual(COUNTER_VALUE, authenticateResponse.getCounter());
			java.security.Signature ecdsaSignature = java.security.Signature.getInstance("SHA256withECDSA"
				);
			ecdsaSignature.initVerify(USER_PUBLIC_KEY_SIGN);
			ecdsaSignature.update(EXPECTED_AUTHENTICATE_SIGNED_BYTES);
			NUnit.Framework.Assert.IsTrue(ecdsaSignature.verify(authenticateResponse.getSignature
				()));
		}
	}*/
}
