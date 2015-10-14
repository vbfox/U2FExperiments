// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.messages;
using NUnit.Framework;
using static BlackFox.U2F.Tests.TestVectors;

namespace BlackFox.U2F.Tests.Codec
{
	public class RawCodecTest
	{
		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeRegisterRequest()
		{
			RegisterRequest registerRequest = new RegisterRequest
				(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256);
			byte[] encodedBytes = RawMessageCodec.EncodeRegisterRequest(
				registerRequest);
            
			CollectionAssert.AreEqual(REGISTRATION_REQUEST_DATA, encodedBytes);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestDecodeRegisterRequest()
		{
			RegisterRequest registerRequest = RawMessageCodec
				.DecodeRegisterRequest(REGISTRATION_REQUEST_DATA);
			Assert.AreEqual(new RegisterRequest(APP_ID_ENROLL_SHA256
				, BROWSER_DATA_ENROLL_SHA256), registerRequest);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeRegisterResponse()
		{
			RegisterResponse registerResponse = new RegisterResponse
				(USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL);
			byte[] encodedBytes = RawMessageCodec.EncodeRegisterResponse
				(registerResponse);
			CollectionAssert.AreEqual(REGISTRATION_RESPONSE_DATA, encodedBytes
				);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeRegisterSignedBytes()
		{
			byte[] encodedBytes = RawMessageCodec.EncodeRegistrationSignedBytes
				(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX
				);
			CollectionAssert.AreEqual(EXPECTED_REGISTER_SIGNED_BYTES, encodedBytes
				);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestDecodeRegisterResponse()
		{
			RegisterResponse registerResponse = RawMessageCodec
				.DecodeRegisterResponse(REGISTRATION_RESPONSE_DATA);
			Assert.AreEqual(new RegisterResponse(
				USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL), registerResponse
				);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeAuthenticateRequest()
		{
			AuthenticateRequest authenticateRequest = new AuthenticateRequest
				(AuthenticateRequest.UserPresenceSign, BROWSER_DATA_SIGN_SHA256
				, APP_ID_SIGN_SHA256, KEY_HANDLE);
			byte[] encodedBytes = RawMessageCodec.EncodeAuthenticateRequest
				(authenticateRequest);
			CollectionAssert.AreEqual(SIGN_REQUEST_DATA, encodedBytes);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestDecodeAuthenticateRequest()
		{
			AuthenticateRequest authenticateRequest = RawMessageCodec
				.DecodeAuthenticateRequest(SIGN_REQUEST_DATA);
			Assert.AreEqual(new AuthenticateRequest
				(AuthenticateRequest.UserPresenceSign, BROWSER_DATA_SIGN_SHA256
				, APP_ID_SIGN_SHA256, KEY_HANDLE), authenticateRequest);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeAuthenticateResponse()
		{
			AuthenticateResponse authenticateResponse = new AuthenticateResponse
				(UserPresenceVerifierConstants.UserPresentFlag, COUNTER_VALUE, SIGNATURE_AUTHENTICATE
				);
			byte[] encodedBytes = RawMessageCodec.EncodeAuthenticateResponse
				(authenticateResponse);
			CollectionAssert.AreEqual(SIGN_RESPONSE_DATA, encodedBytes);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestDecodeAuthenticateResponse()
		{
            
			AuthenticateResponse authenticateResponse = RawMessageCodec
				.DecodeAuthenticateResponse(SIGN_RESPONSE_DATA);
			Assert.AreEqual(new AuthenticateResponse
				(UserPresenceVerifierConstants.UserPresentFlag, COUNTER_VALUE, SIGNATURE_AUTHENTICATE
				), authenticateResponse);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeAuthenticateSignedBytes()
		{
			byte[] encodedBytes = RawMessageCodec.EncodeAuthenticateSignedBytes
				(APP_ID_SIGN_SHA256, UserPresenceVerifierConstants.UserPresentFlag, 
				COUNTER_VALUE, BROWSER_DATA_SIGN_SHA256);
			CollectionAssert.AreEqual(EXPECTED_AUTHENTICATE_SIGNED_BYTES, encodedBytes
				);
		}
	}
}
