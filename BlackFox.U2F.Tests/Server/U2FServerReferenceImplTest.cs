using System.Collections.Generic;
using System.Linq;
using BlackFox.U2F.Server;
using BlackFox.U2F.Server.data;
using BlackFox.U2F.Server.impl;
using BlackFox.U2F.Server.messages;
using Moq;
using NFluent;
using NUnit.Framework;
using Org.BouncyCastle.X509;
using static BlackFox.U2F.Tests.TestVectors;

namespace BlackFox.U2F.Tests.Server
{
    public class U2FServerReferenceImplTest
    {
        private IServerCrypto crypto;
        private Mock<IChallengeGenerator> mockChallengeGenerator;
        private Mock<IDataStore> mockDataStore;
        private Mock<ISessionIdGenerator> mockSessionIdGenerator;

        // ReSharper disable once InconsistentNaming
        private IU2FServer u2fServer;

        /// <exception cref="System.Exception" />
        [SetUp]
        public virtual void Setup()
        {
            mockChallengeGenerator = new Mock<IChallengeGenerator>();
            mockSessionIdGenerator = new Mock<ISessionIdGenerator>();
            mockDataStore = new Mock<IDataStore>();
            crypto = new BouncyCastleServerCrypto();

            var trustedCertificates = new List<X509Certificate> {VENDOR_CERTIFICATE};

            mockChallengeGenerator.Setup(x => x.GenerateChallenge(ACCOUNT_NAME)).Returns(SERVER_CHALLENGE_ENROLL);
            mockSessionIdGenerator.Setup(x => x.GenerateSessionId(ACCOUNT_NAME)).Returns(SESSION_ID);
            mockDataStore.Setup(x => x.StoreSessionData(It.IsAny<EnrollSessionData>())).Returns(SESSION_ID);
            mockDataStore.Setup(x => x.GetTrustedCertificates()).Returns(trustedCertificates);
            mockDataStore.Setup(x => x.GetSecurityKeyData(ACCOUNT_NAME))
                .Returns(
                    new[] {new SecurityKeyData(0L, KEY_HANDLE, USER_PUBLIC_KEY_SIGN_HEX, VENDOR_CERTIFICATE, 0)}.ToList());
        }

        [Test]
        public virtual void TestSanitizeOrigin()
        {
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com"));
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com/"));
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com/foo"));
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com/foo?bar=b"));
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com/foo#fragment"));
            Assert.AreEqual("https://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("https://example.com"));
            Assert.AreEqual("https://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("https://example.com/foo"));
        }

        /// <exception cref="U2FException" />
        [Test]
        public virtual void TestGetRegistrationRequest()
        {
            u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationRequest = u2fServer.GetRegistrationRequest(ACCOUNT_NAME, APP_ID_ENROLL);

            var expected = new RegistrationRequest("U2F_V2", SERVER_CHALLENGE_ENROLL_BASE64, APP_ID_ENROLL, SESSION_ID);
            Check.That(registrationRequest).IsEqualTo(expected);
        }

        /// <exception cref="U2FException" />
        [Test]
        public virtual void TestProcessRegistrationResponse_NoTransports()
        {
            mockDataStore.Setup(x => x.GetEnrollSessionData(SESSION_ID))
                .Returns(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL));
            u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationResponse = new RegistrationResponse(REGISTRATION_DATA_BASE64, BROWSER_DATA_ENROLL_BASE64,
                SESSION_ID);
            u2fServer.ProcessRegistrationResponse(registrationResponse, 0L);

            var expectedKeyData = new SecurityKeyData(0L, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX, VENDOR_CERTIFICATE, 0);
            mockDataStore.Verify(x => x.AddSecurityKeyData(ACCOUNT_NAME, expectedKeyData));
        }

        /// <exception cref="U2FException" />
        [Test]
        public virtual void TestProcessRegistrationResponse_OneTransport()
        {
            mockDataStore.Setup(x => x.GetEnrollSessionData(SESSION_ID))
                .Returns(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL));
            var trustedCertificates = new List<X509Certificate>();
            trustedCertificates.Add(TRUSTED_CERTIFICATE_ONE_TRANSPORT);
            mockDataStore.Setup(x => x.GetTrustedCertificates()).Returns(trustedCertificates);
            u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationResponse = new RegistrationResponse(REGISTRATION_RESPONSE_DATA_ONE_TRANSPORT_BASE64,
                BROWSER_DATA_ENROLL_BASE64, SESSION_ID);
            u2fServer.ProcessRegistrationResponse(registrationResponse, 0L);

            var transports = new List<SecurityKeyDataTransports>();
            transports.Add(SecurityKeyDataTransports.BluetoothRadio);
            var expectedKeyData = new SecurityKeyData(0L, transports, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX,
                TRUSTED_CERTIFICATE_ONE_TRANSPORT, 0);
            mockDataStore.Verify(x => x.AddSecurityKeyData(ACCOUNT_NAME, expectedKeyData));
        }


        /// <exception cref="U2FException" />
        [Test]
        public virtual void TestProcessRegistrationResponse_MultipleTransports()
        {
            mockDataStore.Setup(x => x.GetEnrollSessionData(SESSION_ID))
                .Returns(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL));
            var trustedCertificates = new List<X509Certificate>();
            trustedCertificates.Add(TRUSTED_CERTIFICATE_MULTIPLE_TRANSPORTS);
            mockDataStore.Setup(x => x.GetTrustedCertificates()).Returns(trustedCertificates);
            u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationResponse = new RegistrationResponse(REGISTRATION_RESPONSE_DATA_MULTIPLE_TRANSPORTS_BASE64,
                BROWSER_DATA_ENROLL_BASE64, SESSION_ID);
            u2fServer.ProcessRegistrationResponse(registrationResponse, 0L);

            var transports = new List<SecurityKeyDataTransports>();
            transports.Add(SecurityKeyDataTransports.BluetoothRadio);
            transports.Add(SecurityKeyDataTransports.BluetoothLowEnergy);
            transports.Add(SecurityKeyDataTransports.Nfc);
            var expectedKeyData = new SecurityKeyData(0L, transports, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX,
                TRUSTED_CERTIFICATE_MULTIPLE_TRANSPORTS, 0);
            mockDataStore.Verify(x => x.AddSecurityKeyData(ACCOUNT_NAME, expectedKeyData));
        }


        /// <exception cref="U2FException" />
        [Test]
        public virtual void TestProcessRegistrationResponse_MalformedTransports()
        {
            mockDataStore.Setup(x => x.GetEnrollSessionData(SESSION_ID))
                .Returns(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL));
            var trustedCertificates = new List<X509Certificate>();
            trustedCertificates.Add(TRUSTED_CERTIFICATE_MALFORMED_TRANSPORTS_EXTENSION);
            mockDataStore.Setup(x => x.GetTrustedCertificates()).Returns(trustedCertificates);
            u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationResponse = new RegistrationResponse(REGISTRATION_RESPONSE_DATA_MALFORMED_TRANSPORTS_BASE64,
                BROWSER_DATA_ENROLL_BASE64, SESSION_ID);
            u2fServer.ProcessRegistrationResponse(registrationResponse, 0L);

            var expectedKeyData = new SecurityKeyData
                (0L, null, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX, TRUSTED_CERTIFICATE_MALFORMED_TRANSPORTS_EXTENSION, 0);
            mockDataStore.Verify(x => x.AddSecurityKeyData(ACCOUNT_NAME, expectedKeyData));
        }

        /*
		// transports 
		/// <exception cref="U2FException"/>
		[Test]
		public virtual void testProcessRegistrationResponse2()
		{
			org.mockito.Mockito.when(mockDataStore.getEnrollSessionData(SESSION_ID)).thenReturn
				(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL
				));
			java.util.HashSet<X509Certificate> trustedCertificates = new java.util.HashSet
				<X509Certificate>();
			trustedCertificates.add(VENDOR_CERTIFICATE);
			trustedCertificates.add(TRUSTED_CERTIFICATE_2);
			org.mockito.Mockito.when(mockDataStore.getTrustedCertificates()).thenReturn(trustedCertificates
				);
			u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator
				, mockDataStore.Object, crypto, TRUSTED_DOMAINS);
			RegistrationResponse registrationResponse = new RegistrationResponse
				(REGISTRATION_DATA_2_BASE64, BROWSER_DATA_2_BASE64, SESSION_ID);
			u2fServer.processRegistrationResponse(registrationResponse, 0L);
			org.mockito.Mockito.verify(mockDataStore).addSecurityKeyData(org.mockito.Matchers.eq
				(ACCOUNT_NAME), org.mockito.Matchers.eq(new SecurityKeyData
				(0L, null, KEY_HANDLE_2, USER_PUBLIC_KEY_2, TRUSTED_CERTIFICATE_2, 0)));
		}

		// transports
		/// <exception cref="U2FException"/>
		[Test]
		public virtual void testGetSignRequest()
		{
			u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object
                , mockDataStore.Object, crypto, TRUSTED_DOMAINS);
			org.mockito.Mockito.when(mockChallengeGenerator.generateChallenge(ACCOUNT_NAME)).
				thenReturn(SERVER_CHALLENGE_SIGN);
			IList<SignRequest> signRequest
				 = u2fServer.getSignRequest(ACCOUNT_NAME, APP_ID_SIGN);
			Assert.AreEqual(new SignRequest("U2F_V2"
				, SERVER_CHALLENGE_SIGN_BASE64, APP_ID_SIGN, KEY_HANDLE_BASE64, SESSION_ID), signRequest
				[0]);
		}

		/// <exception cref="U2FException"/>
		[Test]
		public virtual void testProcessSignResponse()
		{
			org.mockito.Mockito.when(mockDataStore.getSignSessionData(SESSION_ID)).thenReturn
				(new SignSessionData(ACCOUNT_NAME, APP_ID_SIGN, SERVER_CHALLENGE_SIGN
				, USER_PUBLIC_KEY_SIGN_HEX));
			u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator
				, mockDataStore, crypto, TRUSTED_DOMAINS);
			SignResponse signResponse = new SignResponse
				(BROWSER_DATA_SIGN_BASE64, SIGN_RESPONSE_DATA_BASE64, SERVER_CHALLENGE_SIGN_BASE64
				, SESSION_ID, APP_ID_SIGN);
			u2fServer.processSignResponse(signResponse);
		}

		/// <exception cref="U2FException"/>
		[Test]
		public virtual void testProcessSignResponse_badOrigin()
		{
			org.mockito.Mockito.when(mockDataStore.getSignSessionData(SESSION_ID)).thenReturn
				(new SignSessionData(ACCOUNT_NAME, APP_ID_SIGN, SERVER_CHALLENGE_SIGN
				, USER_PUBLIC_KEY_SIGN_HEX));
			u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator
				, mockDataStore, crypto, com.google.common.collect.ImmutableSet.of("some-other-domain.com"
				));
			SignResponse signResponse = new SignResponse
				(BROWSER_DATA_SIGN_BASE64, SIGN_RESPONSE_DATA_BASE64, SERVER_CHALLENGE_SIGN_BASE64
				, SESSION_ID, APP_ID_SIGN);
			try
			{
				u2fServer.processSignResponse(signResponse);
				Assert.Fail("expected exception, but didn't get it");
			}
			catch (com.google.u2f.U2FException e)
			{
				Assert.IsTrue(e.Message.contains("is not a recognized home origin"
					));
			}
		}

		// @Test
		// TODO: put test back in once we have signature sample on a correct browserdata json
		// (currently, this test uses an enrollment browserdata during a signature)
		/// <exception cref="U2FException"/>
		public virtual void testProcessSignResponse2()
		{
			org.mockito.Mockito.when(mockDataStore.getSignSessionData(SESSION_ID)).thenReturn
				(new SignSessionData(ACCOUNT_NAME, APP_ID_2, SERVER_CHALLENGE_SIGN
				, USER_PUBLIC_KEY_2));
			org.mockito.Mockito.when(mockDataStore.getSecurityKeyData(ACCOUNT_NAME)).thenReturn
				(com.google.common.collect.ImmutableList.of(new SecurityKeyData
				(0l, KEY_HANDLE_2, USER_PUBLIC_KEY_2, VENDOR_CERTIFICATE, 0)));
			u2fServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object
                , mockDataStore.Object, crypto, TRUSTED_DOMAINS);
			SignResponse signResponse = new SignResponse
				(BROWSER_DATA_2_BASE64, SIGN_DATA_2_BASE64, CHALLENGE_2_BASE64, SESSION_ID, APP_ID_2
				);
			u2fServer.ProcessSignResponse(signResponse);
		}*/
    }
}