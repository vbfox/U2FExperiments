// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

namespace BlackFox.U2F.Tests.Codec
{/*
	public class RawCodecTest : com.google.u2f.TestVectors
	{
		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testEncodeRegisterRequest()
		{
			com.google.u2f.key.messages.RegisterRequest registerRequest = new com.google.u2f.key.messages.RegisterRequest
				(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256);
			byte[] encodedBytes = com.google.u2f.codec.RawMessageCodec.encodeRegisterRequest(
				registerRequest);
			NUnit.Framework.Assert.assertArrayEquals(REGISTRATION_REQUEST_DATA, encodedBytes);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testDecodeRegisterRequest()
		{
			com.google.u2f.key.messages.RegisterRequest registerRequest = com.google.u2f.codec.RawMessageCodec
				.decodeRegisterRequest(REGISTRATION_REQUEST_DATA);
			NUnit.Framework.Assert.AreEqual(new com.google.u2f.key.messages.RegisterRequest(APP_ID_ENROLL_SHA256
				, BROWSER_DATA_ENROLL_SHA256), registerRequest);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testEncodeRegisterResponse()
		{
			com.google.u2f.key.messages.RegisterResponse registerResponse = new com.google.u2f.key.messages.RegisterResponse
				(USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL);
			byte[] encodedBytes = com.google.u2f.codec.RawMessageCodec.encodeRegisterResponse
				(registerResponse);
			NUnit.Framework.Assert.assertArrayEquals(REGISTRATION_RESPONSE_DATA, encodedBytes
				);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testEncodeRegisterSignedBytes()
		{
			byte[] encodedBytes = com.google.u2f.codec.RawMessageCodec.encodeRegistrationSignedBytes
				(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX
				);
			NUnit.Framework.Assert.assertArrayEquals(EXPECTED_REGISTER_SIGNED_BYTES, encodedBytes
				);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testDecodeRegisterResponse()
		{
			com.google.u2f.key.messages.RegisterResponse registerResponse = com.google.u2f.codec.RawMessageCodec
				.decodeRegisterResponse(REGISTRATION_RESPONSE_DATA);
			NUnit.Framework.Assert.AreEqual(new com.google.u2f.key.messages.RegisterResponse(
				USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL), registerResponse
				);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testEncodeAuthenticateRequest()
		{
			com.google.u2f.key.messages.AuthenticateRequest authenticateRequest = new com.google.u2f.key.messages.AuthenticateRequest
				(com.google.u2f.key.messages.AuthenticateRequest.USER_PRESENCE_SIGN, BROWSER_DATA_SIGN_SHA256
				, APP_ID_SIGN_SHA256, KEY_HANDLE);
			byte[] encodedBytes = com.google.u2f.codec.RawMessageCodec.encodeAuthenticateRequest
				(authenticateRequest);
			NUnit.Framework.Assert.assertArrayEquals(SIGN_REQUEST_DATA, encodedBytes);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testDecodeAuthenticateRequest()
		{
			com.google.u2f.key.messages.AuthenticateRequest authenticateRequest = com.google.u2f.codec.RawMessageCodec
				.decodeAuthenticateRequest(SIGN_REQUEST_DATA);
			NUnit.Framework.Assert.AreEqual(new com.google.u2f.key.messages.AuthenticateRequest
				(com.google.u2f.key.messages.AuthenticateRequest.USER_PRESENCE_SIGN, BROWSER_DATA_SIGN_SHA256
				, APP_ID_SIGN_SHA256, KEY_HANDLE), authenticateRequest);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testEncodeAuthenticateResponse()
		{
			com.google.u2f.key.messages.AuthenticateResponse authenticateResponse = new com.google.u2f.key.messages.AuthenticateResponse
				(com.google.u2f.key.UserPresenceVerifier.USER_PRESENT_FLAG, COUNTER_VALUE, SIGNATURE_AUTHENTICATE
				);
			byte[] encodedBytes = com.google.u2f.codec.RawMessageCodec.encodeAuthenticateResponse
				(authenticateResponse);
			NUnit.Framework.Assert.assertArrayEquals(SIGN_RESPONSE_DATA, encodedBytes);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testDecodeAuthenticateResponse()
		{
			com.google.u2f.key.messages.AuthenticateResponse authenticateResponse = com.google.u2f.codec.RawMessageCodec
				.decodeAuthenticateResponse(SIGN_RESPONSE_DATA);
			NUnit.Framework.Assert.AreEqual(new com.google.u2f.key.messages.AuthenticateResponse
				(com.google.u2f.key.UserPresenceVerifier.USER_PRESENT_FLAG, COUNTER_VALUE, SIGNATURE_AUTHENTICATE
				), authenticateResponse);
		}

		/// <exception cref="System.Exception"/>
		[NUnit.Framework.Test]
		public virtual void testEncodeAuthenticateSignedBytes()
		{
			byte[] encodedBytes = com.google.u2f.codec.RawMessageCodec.encodeAuthenticateSignedBytes
				(APP_ID_SIGN_SHA256, com.google.u2f.key.UserPresenceVerifier.USER_PRESENT_FLAG, 
				COUNTER_VALUE, BROWSER_DATA_SIGN_SHA256);
			NUnit.Framework.Assert.assertArrayEquals(EXPECTED_AUTHENTICATE_SIGNED_BYTES, encodedBytes
				);
		}
	}*/
}
