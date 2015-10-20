// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

using BlackFox.Binary;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.messages;
using NUnit.Framework;
using static BlackFox.U2F.Tests.TestVectors;

namespace BlackFox.U2F.Tests.Codec
{
    public class SerialCodecTest
    {
        [Test]
        public virtual void TestEncodeRegisterRequest()
        {
            var registerRequest = new RegisterRequest(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256);
            var encodedBytes = RawMessageCodec.EncodeRegisterRequest(registerRequest);

            CollectionAssert.AreEqual(REGISTRATION_REQUEST_DATA, encodedBytes);
        }

        [Test]
        public virtual void TestDecodeRegisterRequest()
        {
            var registerRequest = RawMessageCodec.DecodeRegisterRequest(REGISTRATION_REQUEST_DATA);
            Assert.AreEqual(new RegisterRequest(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256), registerRequest);
        }

        [Test]
        public virtual void TestEncodeRegisterResponse()
        {
            var registerResponse = new RegisterResponse(USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE,
                SIGNATURE_ENROLL);
            var encodedBytes = RawMessageCodec.EncodeRegisterResponse(registerResponse);
            CollectionAssert.AreEqual(REGISTRATION_RESPONSE_DATA, encodedBytes);
        }

        [Test]
        public virtual void TestEncodeRegisterSignedBytes()
        {
            var encodedBytes = RawMessageCodec.EncodeRegistrationSignedBytes(APP_ID_ENROLL_SHA256,
                BROWSER_DATA_ENROLL_SHA256, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX);
            CollectionAssert.AreEqual(EXPECTED_REGISTER_SIGNED_BYTES, encodedBytes);
        }

        [Test]
        public virtual void TestDecodeRegisterResponse()
        {
            var registerResponse = RawMessageCodec.DecodeRegisterResponse(REGISTRATION_RESPONSE_DATA.Segment());
            Assert.AreEqual(
                new RegisterResponse(USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL),
                registerResponse);
        }

        [Test]
        public virtual void TestEncodeAuthenticateRequest()
        {
            var authenticateRequest = new AuthenticateRequest(U2FVersion.V2, AuthenticateRequest.UserPresenceSign,
                BROWSER_DATA_SIGN_SHA256, APP_ID_SIGN_SHA256, KEY_HANDLE);
            var encodedBytes = RawMessageCodec.EncodeAuthenticateRequest(authenticateRequest, U2FVersion.V2);
            CollectionAssert.AreEqual(SIGN_REQUEST_DATA, encodedBytes);
        }

        [Test]
        public virtual void TestDecodeAuthenticateRequest()
        {
            var authenticateRequest = RawMessageCodec.DecodeAuthenticateRequest(SIGN_REQUEST_DATA);
            Assert.AreEqual(
                new AuthenticateRequest(U2FVersion.V2, AuthenticateRequest.UserPresenceSign, BROWSER_DATA_SIGN_SHA256,
                    APP_ID_SIGN_SHA256, KEY_HANDLE), authenticateRequest);
        }

        [Test]
        public virtual void TestEncodeAuthenticateResponse()
        {
            var authenticateResponse = new AuthenticateResponse(UserPresenceVerifierConstants.UserPresentFlag,
                COUNTER_VALUE, SIGNATURE_AUTHENTICATE);
            var encodedBytes = RawMessageCodec.EncodeAuthenticateResponse(authenticateResponse);
            CollectionAssert.AreEqual(SIGN_RESPONSE_DATA, encodedBytes);
        }

        [Test]
        public virtual void TestDecodeAuthenticateResponse()
        {
            var authenticateResponse = RawMessageCodec.DecodeAuthenticateResponse(SIGN_RESPONSE_DATA.Segment());
            Assert.AreEqual(
                new AuthenticateResponse(UserPresenceVerifierConstants.UserPresentFlag, COUNTER_VALUE,
                    SIGNATURE_AUTHENTICATE), authenticateResponse);
        }

        [Test]
        public virtual void TestEncodeAuthenticateSignedBytes()
        {
            var encodedBytes = RawMessageCodec.EncodeAuthenticateSignedBytes(APP_ID_SIGN_SHA256,
                UserPresenceVerifierConstants.UserPresentFlag, COUNTER_VALUE, BROWSER_DATA_SIGN_SHA256);
            CollectionAssert.AreEqual(EXPECTED_AUTHENTICATE_SIGNED_BYTES, encodedBytes);
        }
    }
}