// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

using BlackFox.Binary;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Gnubby.Messages;
using BlackFox.U2F.Gnubby.Simulated;
using BlackFox.U2F.Key;
using NUnit.Framework;
using static BlackFox.U2F.Tests.TestVectors;

namespace BlackFox.U2F.Tests.Codec
{
    public class SerialCodecTest
    {
        [Test]
        public virtual void TestEncodeRegisterRequest()
        {
            var registerRequest = new KeyRegisterRequest(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256);
            var encodedBytes = RawMessageCodec.EncodeKeyRegisterRequest(registerRequest);

            CollectionAssert.AreEqual(REGISTRATION_REQUEST_DATA, encodedBytes);
        }

        [Test]
        public virtual void TestDecodeRegisterRequest()
        {
            var registerRequest = RawMessageCodec.DecodeKeyRegisterRequest(REGISTRATION_REQUEST_DATA);
            Assert.AreEqual(new KeyRegisterRequest(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256), registerRequest);
        }

        [Test]
        public virtual void TestEncodeRegisterResponse()
        {
            var registerResponse = new KeyRegisterResponse(USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE,
                SIGNATURE_ENROLL);
            var encodedBytes = RawMessageCodec.EncodeKeyRegisterResponse(registerResponse);
            CollectionAssert.AreEqual(REGISTRATION_RESPONSE_DATA, encodedBytes);
        }

        [Test]
        public virtual void TestEncodeRegisterSignedBytes()
        {
            var encodedBytes = RawMessageCodec.EncodeKeyRegisterSignedBytes(APP_ID_ENROLL_SHA256,
                BROWSER_DATA_ENROLL_SHA256, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX);
            CollectionAssert.AreEqual(EXPECTED_REGISTER_SIGNED_BYTES, encodedBytes);
        }

        [Test]
        public virtual void TestDecodeRegisterResponse()
        {
            var registerResponse = RawMessageCodec.DecodeKeyRegisterResponse(REGISTRATION_RESPONSE_DATA.Segment());
            Assert.AreEqual(
                new KeyRegisterResponse(USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL),
                registerResponse);
        }

        [Test]
        public virtual void TestEncodeAuthenticateRequest()
        {
            var authenticateRequest = new KeySignRequest(U2FVersion.V2,
                BROWSER_DATA_SIGN_SHA256, APP_ID_SIGN_SHA256, KEY_HANDLE);
            var encodedBytes = RawMessageCodec.EncodeKeySignRequest(authenticateRequest);
            CollectionAssert.AreEqual(SIGN_REQUEST_DATA, encodedBytes);
        }

        [Test]
        public virtual void TestDecodeAuthenticateRequest()
        {
            var authenticateRequest = RawMessageCodec.DecodeKeySignRequest(SIGN_REQUEST_DATA);
            Assert.AreEqual(
                new KeySignRequest(U2FVersion.V2, BROWSER_DATA_SIGN_SHA256,
                    APP_ID_SIGN_SHA256, KEY_HANDLE), authenticateRequest);
        }

        [Test]
        public virtual void TestEncodeAuthenticateResponse()
        {
            var authenticateResponse = new KeySignResponse(UserPresenceVerifierConstants.UserPresentFlag,
                COUNTER_VALUE, SIGNATURE_AUTHENTICATE);
            var encodedBytes = RawMessageCodec.EncodeKeySignResponse(authenticateResponse);
            CollectionAssert.AreEqual(SIGN_RESPONSE_DATA, encodedBytes);
        }

        [Test]
        public virtual void TestDecodeAuthenticateResponse()
        {
            var authenticateResponse = RawMessageCodec.DecodeKeySignResponse(SIGN_RESPONSE_DATA.Segment());
            Assert.AreEqual(
                new KeySignResponse(UserPresenceVerifierConstants.UserPresentFlag, COUNTER_VALUE,
                    SIGNATURE_AUTHENTICATE), authenticateResponse);
        }

        [Test]
        public virtual void TestEncodeAuthenticateSignedBytes()
        {
            var encodedBytes = RawMessageCodec.EncodeKeySignSignedBytes(APP_ID_SIGN_SHA256,
                UserPresenceVerifierConstants.UserPresentFlag, COUNTER_VALUE, BROWSER_DATA_SIGN_SHA256);
            CollectionAssert.AreEqual(EXPECTED_AUTHENTICATE_SIGNED_BYTES, encodedBytes);
        }
    }
}