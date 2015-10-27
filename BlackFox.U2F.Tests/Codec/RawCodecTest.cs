// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using NUnit.Framework;
using static BlackFox.U2F.Tests.TestVectors;
using BlackFox.Binary;
using BlackFox.U2F.Gnubby.Messages;
using BlackFox.U2F.Gnubby.Simulated;

namespace BlackFox.U2F.Tests.Codec
{
	public class RawCodecTest
	{
		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeRegisterRequest()
		{
			KeyRegisterRequest keyRegisterRequest = new KeyRegisterRequest
				(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256);
			byte[] encodedBytes = RawMessageCodec.EncodeKeyRegisterRequest(
				keyRegisterRequest);
            
			CollectionAssert.AreEqual(REGISTRATION_REQUEST_DATA, encodedBytes);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestDecodeRegisterRequest()
		{
			KeyRegisterRequest keyRegisterRequest = RawMessageCodec
				.DecodeKeyRegisterRequest(REGISTRATION_REQUEST_DATA);
			Assert.AreEqual(new KeyRegisterRequest(APP_ID_ENROLL_SHA256
				, BROWSER_DATA_ENROLL_SHA256), keyRegisterRequest);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeRegisterResponse()
		{
			KeyRegisterResponse keyRegisterResponse = new KeyRegisterResponse
				(USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL);
			byte[] encodedBytes = RawMessageCodec.EncodeKeyRegisterResponse
				(keyRegisterResponse);
			CollectionAssert.AreEqual(REGISTRATION_RESPONSE_DATA, encodedBytes
				);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeRegisterSignedBytes()
		{
			byte[] encodedBytes = RawMessageCodec.EncodeKeyRegisterSignedBytes
				(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX
				);
			CollectionAssert.AreEqual(EXPECTED_REGISTER_SIGNED_BYTES, encodedBytes
				);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestDecodeRegisterResponse()
		{
			KeyRegisterResponse keyRegisterResponse = RawMessageCodec
				.DecodeKeyRegisterResponse(REGISTRATION_RESPONSE_DATA.Segment());
			Assert.AreEqual(new KeyRegisterResponse(
				USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL), keyRegisterResponse
				);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeAuthenticateRequest()
		{
			KeySignRequest keySignRequest = new KeySignRequest
				(U2FVersion.V2, BROWSER_DATA_SIGN_SHA256
				, APP_ID_SIGN_SHA256, KEY_HANDLE);
			byte[] encodedBytes = RawMessageCodec.EncodeKeySignRequest(keySignRequest);
			CollectionAssert.AreEqual(SIGN_REQUEST_DATA, encodedBytes);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestDecodeAuthenticateRequest()
		{
			KeySignRequest keySignRequest = RawMessageCodec
				.DecodeKeySignRequest(SIGN_REQUEST_DATA);
			Assert.AreEqual(new KeySignRequest
				(U2FVersion.V2, BROWSER_DATA_SIGN_SHA256
				, APP_ID_SIGN_SHA256, KEY_HANDLE), keySignRequest);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeAuthenticateResponse()
		{
			var authenticateResponse = new KeySignResponse(UserPresenceVerifierConstants.UserPresentFlag, COUNTER_VALUE, SIGNATURE_AUTHENTICATE);
			byte[] encodedBytes = RawMessageCodec.EncodeKeySignResponse
				(authenticateResponse);
			CollectionAssert.AreEqual(SIGN_RESPONSE_DATA, encodedBytes);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestDecodeAuthenticateResponse()
		{
            
			KeySignResponse keySignResponse = RawMessageCodec
				.DecodeKeySignResponse(SIGN_RESPONSE_DATA.Segment());
			Assert.AreEqual(new KeySignResponse(UserPresenceVerifierConstants.UserPresentFlag, COUNTER_VALUE, SIGNATURE_AUTHENTICATE), keySignResponse);
		}

		/// <exception cref="System.Exception"/>
		[Test]
		public virtual void TestEncodeAuthenticateSignedBytes()
		{
			byte[] encodedBytes = RawMessageCodec.EncodeKeySignSignedBytes
				(APP_ID_SIGN_SHA256, UserPresenceVerifierConstants.UserPresentFlag, 
				COUNTER_VALUE, BROWSER_DATA_SIGN_SHA256);
			CollectionAssert.AreEqual(EXPECTED_AUTHENTICATE_SIGNED_BYTES, encodedBytes
				);
		}
	}
}
