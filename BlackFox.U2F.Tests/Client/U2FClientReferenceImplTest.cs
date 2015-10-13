// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

namespace BlackFox.U2F.Tests.Client
{/*
	public class U2FClientReferenceImplTest : com.google.u2f.TestVectors
	{
		[org.mockito.Mock]
		internal com.google.u2f.key.U2FKey mockU2fKey;

		[org.mockito.Mock]
		internal com.google.u2f.server.U2FServer mockU2fServer;

		[org.mockito.Mock]
		internal com.google.u2f.client.OriginVerifier mockOriginVerifier;

		[org.mockito.Mock]
		internal com.google.u2f.client.ChannelIdProvider mockChannelIdProvider;

		private com.google.u2f.client.impl.U2FClientReferenceImpl u2fClient;

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.SetUp]
		public virtual void setup()
		{
			org.mockito.MockitoAnnotations.initMocks(this);
			u2fClient = new com.google.u2f.client.impl.U2FClientReferenceImpl(new com.google.u2f.client.impl.CryptoImpl
				(), mockOriginVerifier, mockChannelIdProvider, mockU2fServer, mockU2fKey);
			org.mockito.Mockito.when(mockChannelIdProvider.getJsonChannelId()).thenReturn(CHANNEL_ID_JSON
				);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testRegister()
		{
			org.mockito.Mockito.when(mockU2fServer.getRegistrationRequest(ACCOUNT_NAME, APP_ID_ENROLL
				)).thenReturn(new RegistrationRequest(com.google.u2f.U2FConsts
				.U2F_V2, SERVER_CHALLENGE_ENROLL_BASE64, APP_ID_ENROLL, SESSION_ID));
			org.mockito.Mockito.doNothing().when(mockOriginVerifier).validateOrigin(APP_ID_ENROLL
				, ORIGIN);
			org.mockito.Mockito.when(mockU2fKey.register(new com.google.u2f.key.messages.RegisterRequest
				(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256))).thenReturn(new com.google.u2f.key.messages.RegisterResponse
				(USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL));
			org.mockito.Mockito.when(mockU2fServer.processRegistrationResponse(new RegistrationResponse
				(REGISTRATION_DATA_BASE64, BROWSER_DATA_ENROLL_BASE64, SESSION_ID), 0L)).thenReturn
				(new SecurityKeyData(0L, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX
				, VENDOR_CERTIFICATE, 0));
			u2fClient.register(ORIGIN, ACCOUNT_NAME);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testAuthenticate()
		{
			org.mockito.Mockito.when(mockU2fServer.getSignRequest(ACCOUNT_NAME, ORIGIN)).thenReturn
				(com.google.common.collect.ImmutableList.of(new SignRequest
				(com.google.u2f.U2FConsts.U2F_V2, SERVER_CHALLENGE_SIGN_BASE64, APP_ID_SIGN, KEY_HANDLE_BASE64
				, SESSION_ID)));
			org.mockito.Mockito.doNothing().when(mockOriginVerifier).validateOrigin(APP_ID_SIGN
				, ORIGIN);
			org.mockito.Mockito.when(mockU2fKey.authenticate(new com.google.u2f.key.messages.AuthenticateRequest
				(com.google.u2f.key.UserPresenceVerifier.USER_PRESENT_FLAG, BROWSER_DATA_SIGN_SHA256
				, APP_ID_SIGN_SHA256, KEY_HANDLE))).thenReturn(new com.google.u2f.key.messages.AuthenticateResponse
				(com.google.u2f.key.UserPresenceVerifier.USER_PRESENT_FLAG, COUNTER_VALUE, SIGNATURE_AUTHENTICATE
				));
			org.mockito.Mockito.when(mockU2fServer.processSignResponse(new SignResponse
				(BROWSER_DATA_SIGN_BASE64, SIGN_RESPONSE_DATA_BASE64, SERVER_CHALLENGE_SIGN_BASE64
				, SESSION_ID, APP_ID_SIGN))).thenReturn(new SecurityKeyData
				(0L, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX, VENDOR_CERTIFICATE, 0));
			u2fClient.authenticate(ORIGIN, ACCOUNT_NAME);
		}
	}*/
}
