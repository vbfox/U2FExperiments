using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby.Messages;
using BlackFox.U2F.Gnubby.Simulated;
using BlackFox.U2F.GnubbyApi;
using BlackFox.U2F.Server.messages;
using Moq;
using NodaTime;
using NUnit.Framework;
using static BlackFox.U2F.Tests.TestVectors;

namespace BlackFox.U2F.Tests.GnubbyApi
{
    [TestFixture]
    public class U2FClientTests
    {
        private Mock<ISender> sender;

        private Mock<IKeyOperations> keyOperations;

        private U2FClient u2FClient;

        [SetUp]
        public virtual void Setup()
        {
            keyOperations = new Mock<IKeyOperations>(MockBehavior.Strict);
            sender = new Mock<ISender>(MockBehavior.Strict);
            sender.SetupGet(x => x.ChannelId).Returns(CHANNEL_ID_JSON);
            sender.SetupGet(x => x.Origin).Returns(ORIGIN);

            var mockClock = new Mock<IClock>(MockBehavior.Strict);
            mockClock.Setup(x => x.Now).Returns(Instant.FromMillisecondsSinceUnixEpoch(0));
            u2FClient = new U2FClient(sender.Object, keyOperations.Object);
        }

        [Test]
        public async Task Sign()
        {
            var keySignRequests = new[]
            {
                new KeySignRequest(U2FVersion.V2, BROWSER_DATA_SIGN_SHA256, APP_ID_SIGN_SHA256, KEY_HANDLE)
            };

            keyOperations
                .Setup(
                    x => x.Sign(
                        It.Is<ICollection<KeySignRequest>>(reqs => reqs.SequenceEqual(keySignRequests)),
                        It.IsAny<CancellationToken>(),
                        false))
                .Returns(
                    (ICollection<KeySignRequest> reqs, CancellationToken ct, bool individualAttest) =>
                        Task.FromResult(
                            SignOperationResult.Success(
                                reqs.Single(),
                                new KeySignResponse(
                                    UserPresenceVerifierConstants.UserPresentFlag,
                                    COUNTER_VALUE,
                                    SIGNATURE_AUTHENTICATE))));

            var signRequest = new SignRequest(U2FConsts.U2Fv2, SERVER_CHALLENGE_SIGN_BASE64, APP_ID_SIGN, KEY_HANDLE_BASE64, SESSION_ID);

            var result = await u2FClient.Sign(new[] {signRequest}, CancellationToken.None);

            Assert.AreEqual(new SignResponse
                (BROWSER_DATA_SIGN_BASE64, SIGN_RESPONSE_DATA_BASE64, SERVER_CHALLENGE_SIGN_BASE64
                    , SESSION_ID, APP_ID_SIGN), result);
        }

        [Test]
        public async Task Register()
        {
            var keySignRequests = new[] { new KeySignRequest(U2FVersion.V2, BROWSER_DATA_SIGN_SHA256, APP_ID_SIGN_SHA256, KEY_HANDLE) };
            var keyRegisterRequests = new[] {new KeyRegisterRequest(APP_ID_ENROLL_SHA256, BROWSER_DATA_ENROLL_SHA256)};

            keyOperations
                .Setup(
                    x => x.Register(
                        It.Is<ICollection<KeyRegisterRequest>>(reqs => reqs.SequenceEqual(keyRegisterRequests)),
                        It.Is<ICollection<KeySignRequest>>(reqs => reqs.SequenceEqual(keySignRequests)),
                        It.IsAny<CancellationToken>()))
                .Returns(
                    (ICollection<KeyRegisterRequest> registerReqs, ICollection<KeySignRequest> signReqs, CancellationToken ct) =>
                        Task.FromResult(
                            RegisterOperationResult.Success(
                                registerReqs.Single(),
                                new KeyRegisterResponse(
                                    USER_PUBLIC_KEY_ENROLL_HEX,
                                    KEY_HANDLE,
                                    VENDOR_CERTIFICATE,
                                    SIGNATURE_ENROLL))));

            var signRequest = new SignRequest(U2FConsts.U2Fv2, SERVER_CHALLENGE_SIGN_BASE64, APP_ID_SIGN, KEY_HANDLE_BASE64, SESSION_ID);
            var registerRequest = new RegisterRequest(U2FConsts.U2Fv2, SERVER_CHALLENGE_ENROLL_BASE64, APP_ID_ENROLL, SESSION_ID);

            var result = await u2FClient.Register(new [] { registerRequest }, new[] { signRequest }, CancellationToken.None);

            Assert.AreEqual(new RegisterResponse(REGISTRATION_DATA_BASE64, BROWSER_DATA_ENROLL_BASE64, SESSION_ID), result);
        }
    }
}
