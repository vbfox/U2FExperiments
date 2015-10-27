// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;
using BlackFox.U2F.Gnubby.Simulated;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.impl;
using Moq;
using NUnit.Framework;
using Org.BouncyCastle.Security;
using static BlackFox.U2F.Tests.TestVectors;

namespace BlackFox.U2F.Tests.Key
{
    public class U2FKeyReferenceImplTest
    {
        private Mock<IKeyDataStore> mockDataStore;

        private Mock<IKeyHandleGenerator> mockKeyHandleGenerator;
        private Mock<IKeyPairGenerator> mockKeyPairGenerator;

        private Mock<IUserPresenceVerifier> mockUserPresenceVerifier;

        private IU2FKey u2FKey;

        [SetUp]
        public virtual void Setup()
        {
            mockKeyPairGenerator = new Mock<IKeyPairGenerator>();
            mockKeyHandleGenerator = new Mock<IKeyHandleGenerator>();
            mockDataStore = new Mock<IKeyDataStore>();
            mockUserPresenceVerifier = new Mock<IUserPresenceVerifier>();

            u2FKey = new U2FKeyReferenceImpl(VENDOR_CERTIFICATE, VENDOR_CERTIFICATE_PRIVATE_KEY,
                mockKeyPairGenerator.Object, mockKeyHandleGenerator.Object, mockDataStore.Object,
                mockUserPresenceVerifier.Object, new BouncyCastleKeyCrypto());

            mockUserPresenceVerifier.Setup(x => x.VerifyUserPresence())
                .Returns(UserPresenceVerifierConstants.UserPresentFlag);
            mockKeyPairGenerator.Setup(x => x.GenerateKeyPair(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256))
                .Returns(USER_KEY_PAIR_ENROLL);
            mockKeyPairGenerator.Setup(x => x.EncodePublicKey(USER_PUBLIC_KEY_ENROLL))
                .Returns(USER_PUBLIC_KEY_ENROLL_HEX);
            mockKeyHandleGenerator.Setup(x => x.GenerateKeyHandle(APP_ID_ENROLL_SHA256, USER_KEY_PAIR_ENROLL))
                .Returns(KEY_HANDLE);
            mockDataStore.Setup(x => x.GetKeyPair(KEY_HANDLE)).Returns(USER_KEY_PAIR_SIGN);
            mockDataStore.Setup(x => x.IncrementCounter()).Returns(COUNTER_VALUE);
        }

        /// <exception cref="System.Exception" />
        [Test]
        public virtual void TestRegister()
        {
            var registerRequest = new KeyRegisterRequest(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256);
            var registerResponse = u2FKey.Register(registerRequest);

            mockDataStore.Verify(x => x.StoreKeyPair(KEY_HANDLE, USER_KEY_PAIR_ENROLL));
            CollectionAssert.AreEqual(USER_PUBLIC_KEY_ENROLL_HEX, registerResponse.UserPublicKey);
            Assert.AreEqual(VENDOR_CERTIFICATE, registerResponse.AttestationCertificate);
            CollectionAssert.AreEqual(KEY_HANDLE, registerResponse.KeyHandle);
            var ecdsaSignature = SignerUtilities.GetSigner("SHA-256withECDSA");
            ecdsaSignature.Init(false, VENDOR_CERTIFICATE.GetPublicKey());
            ecdsaSignature.BlockUpdate(EXPECTED_REGISTER_SIGNED_BYTES, 0, EXPECTED_REGISTER_SIGNED_BYTES.Length);
            Assert.IsTrue(ecdsaSignature.VerifySignature(registerResponse.Signature));
        }


        /// <exception cref="System.Exception" />
        [Test]
        public virtual void TestAuthenticate()
        {
            var authenticateRequest = new KeySignRequest(U2FVersion.V2,
                BROWSER_DATA_SIGN_SHA256, APP_ID_SIGN_SHA256, KEY_HANDLE);
            var authenticateResponse = u2FKey.Authenticate(authenticateRequest);

            Assert.AreEqual(UserPresenceVerifierConstants.UserPresentFlag, authenticateResponse.UserPresence);
            Assert.AreEqual(COUNTER_VALUE, authenticateResponse.Counter);
            var ecdsaSignature = SignerUtilities.GetSigner("SHA-256withECDSA");
            ecdsaSignature.Init(false, USER_PUBLIC_KEY_SIGN);
            ecdsaSignature.BlockUpdate(EXPECTED_AUTHENTICATE_SIGNED_BYTES, 0, EXPECTED_AUTHENTICATE_SIGNED_BYTES.Length);
            Assert.IsTrue(ecdsaSignature.VerifySignature(authenticateResponse.Signature));
        }
    }
}