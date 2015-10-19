// Copyright 2014 Google Inc. All rights reserved.
//
// Use of this source code is governed by a BSD-style
// license that can be found in the LICENSE file or at
// https://developers.google.com/open-source/licenses/bsd

using System.Threading;
using BlackFox.U2F.Client;
using BlackFox.U2F.Client.impl;
using BlackFox.U2F.Server.data;
using BlackFox.U2F.Server.messages;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.messages;
using BlackFox.U2F.Server;
using Moq;
using NodaTime;
using NUnit.Framework;
using static BlackFox.U2F.Tests.TestVectors;

namespace BlackFox.U2F.Tests.Client
{
	public class U2FClientReferenceImplTest
	{
        Mock<IU2FKey> mockU2FKey;

		Mock<IU2FServer> mockU2FServer;

        Mock<IOriginVerifier> mockOriginVerifier;

        Mock<IChannelIdProvider> mockChannelIdProvider;

		private U2FClientReferenceImpl u2FClient;

		[SetUp]
		public virtual void Setup()
		{
		    mockU2FKey = new Mock<IU2FKey>(MockBehavior.Strict);
		    mockU2FServer = new Mock<IU2FServer>(MockBehavior.Strict);
		    mockOriginVerifier = new Mock<IOriginVerifier>(MockBehavior.Strict);
		    mockChannelIdProvider = new Mock<IChannelIdProvider>(MockBehavior.Strict);

		    var mockClock = new Mock<IClock>(MockBehavior.Strict);
		    mockClock.Setup(x => x.Now).Returns(Instant.FromMillisecondsSinceUnixEpoch(0));
            u2FClient = new U2FClientReferenceImpl(new BouncyCastleClientCrypto(), mockOriginVerifier.Object,
                mockChannelIdProvider.Object, mockU2FServer.Object, mockU2FKey.Object, mockClock.Object);

		    mockChannelIdProvider.Setup(x => x.GetJsonChannelId()).Returns(CHANNEL_ID_JSON);
		}

		[Test]
		public virtual void TestRegister()
		{
		    mockU2FServer.Setup(x => x.GetRegistrationRequest(ACCOUNT_NAME, APP_ID_ENROLL))
		        .Returns(new RegistrationRequest(U2FConsts.U2Fv2, SERVER_CHALLENGE_ENROLL_BASE64, APP_ID_ENROLL, SESSION_ID));

		    mockOriginVerifier.Setup(x => x.ValidateOrigin(APP_ID_ENROLL, ORIGIN));
		    mockU2FKey.Setup(x => x.Register(new RegisterRequest(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256)))
		        .Returns(new RegisterResponse(USER_PUBLIC_KEY_ENROLL_HEX, KEY_HANDLE, VENDOR_CERTIFICATE, SIGNATURE_ENROLL));
            mockU2FServer.Setup(x => x.ProcessRegistrationResponse(new RegistrationResponse(REGISTRATION_DATA_BASE64, BROWSER_DATA_ENROLL_BASE64, SESSION_ID), 0L))
                .Returns(new SecurityKeyData(0L, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX, VENDOR_CERTIFICATE, 0));
			u2FClient.Register(ORIGIN, ACCOUNT_NAME);
		}
        
		[Test]
		public virtual void TestAuthenticate()
		{
		    var signRequest = new SignRequest(U2FConsts.U2Fv2, SERVER_CHALLENGE_SIGN_BASE64, APP_ID_SIGN, KEY_HANDLE_BASE64, SESSION_ID);
		    mockU2FServer.Setup(x => x.GetSignRequest(ACCOUNT_NAME, ORIGIN))
		        .Returns(new[] { signRequest });
		    mockOriginVerifier.Setup(x => x.ValidateOrigin(APP_ID_SIGN, ORIGIN));
            mockU2FKey.Setup(x => x.Authenticate(new AuthenticateRequest(UserPresenceVerifierConstants.UserPresentFlag, BROWSER_DATA_SIGN_SHA256, APP_ID_SIGN_SHA256, KEY_HANDLE)))
                .Returns(new AuthenticateResponse(UserPresenceVerifierConstants.UserPresentFlag, COUNTER_VALUE, SIGNATURE_AUTHENTICATE));
            mockU2FServer.Setup(x => x.ProcessSignResponse(new SignResponse
                (BROWSER_DATA_SIGN_BASE64, SIGN_RESPONSE_DATA_BASE64, SERVER_CHALLENGE_SIGN_BASE64
                , SESSION_ID, APP_ID_SIGN))).Returns(new SecurityKeyData
				(0L, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX, VENDOR_CERTIFICATE, 0));

			u2FClient.Authenticate(ORIGIN, ACCOUNT_NAME);
		}
    }
}
