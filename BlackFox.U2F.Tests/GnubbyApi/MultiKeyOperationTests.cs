using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.GnubbyApi;
using Moq;
using NUnit.Framework;

namespace BlackFox.U2F.Tests.GnubbyApi
{
    [TestFixture, Timeout(10*1000)]
    public class MultiKeyOperationTests
    {
        private Mock<IKeyFactory> keyFactory;
        private TestKeyId testKeyId = new TestKeyId("Hello", "World");

        private class TestKeyId : IKeyId
        {
            public TestKeyId(string product, string manufacturer)
            {
                Product = product;
                Manufacturer = manufacturer;
            }

            public bool Equals(IKeyId other)
            {
                return other.Product == Product && other.Manufacturer == Manufacturer;
            }

            public string Product { get; }
            public string Manufacturer { get; }

            public Task<IKey> OpenAsync(CancellationToken cancellationToken = new CancellationToken())
            {
                throw new NotImplementedException();
            }
        }

        [SetUp]
        public void SetUp()
        {
            keyFactory = new Mock<IKeyFactory>(MockBehavior.Strict);
        }

        [Test, ExpectedException(typeof(TaskCanceledException))]
        public async Task StopsWhenTokenIsCancelled()
        {
            keyFactory
                .Setup(x => x.FindAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IKeyId>());

            var op = new MultiKeyOperation<int>(keyFactory.Object,
                (keyId, ct) =>
                {
                    Assert.Fail();
                    return Task.FromResult(0);
                },
                i =>
                {
                    Assert.Fail();
                    return false;
                });

            var token = new CancellationTokenSource();

            var task = op.RunOperationAsync(token.Token);
            token.Cancel();
            await task;
        }

        [Test]
        public async Task CallOperationWithKey()
        {
            keyFactory
                .Setup(x => x.FindAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IKeyId> { testKeyId });

            var op = new MultiKeyOperation<int>(keyFactory.Object,
                (keyId, ct) =>
                {
                    Assert.AreEqual(testKeyId, keyId);
                    return Task.FromResult(42);
                },
                i => i == 42);

            var token = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var result = await op.RunOperationAsync(token.Token);

            Assert.AreEqual(42, result);
        }

        [Test]
        public async Task DiscoverNewKeys()
        {
            int counter = 0;
            keyFactory
                .Setup(x => x.FindAllAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken ct) =>
                {
                    if (counter++ == 2)
                    {
                        return Task.FromResult((ICollection<IKeyId>)new List<IKeyId> { testKeyId });
                    }
                    return Task.FromResult((ICollection<IKeyId>)new List<IKeyId>());
                });

            var op = new MultiKeyOperation<int>(keyFactory.Object,
                (keyId, ct) =>
                {
                    Assert.AreEqual(testKeyId, keyId);
                    return Task.FromResult(42);
                },
                i => i == 42,
                TimeSpan.FromMilliseconds(100),
                TimeSpan.FromMilliseconds(100));

            var token = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var result = await op.RunOperationAsync(token.Token);

            Assert.AreEqual(42, result);
        }
    }
}
