using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackFox.Binary;
using BlackFox.U2FHid.Core;
using BlackFox.UsbHid;
using Moq;
using NFluent;
using NUnit.Framework;
using static BlackFox.U2FHid.Tests.BinaryHelper;

#pragma warning disable IDE0004

namespace BlackFox.U2FHid.Tests
{
    [TestFixture]
    [SuppressMessage("ReSharper", "RedundantCast")]
    public class U2FDeviceTests
    {
        [Test]
        public void WinkSendCorrectBytes()
        {
            // Setup
            var scenario = HidScenario.Build();
            scenario.Write(report => AssertWriteInitPacket(report.Data, TestChannel, U2FHidCommand.Wink));
            scenario.Read(() => new HidInputReport(BuildInitPacket(TestChannel, U2FHidCommand.Wink)));

            var hid = CreateHidMock();
            var device = new U2FHidKey(hid.Object, false);
            InitDevice(hid, device);

            scenario.Run(hid);

            // Act
            device.WinkAsync().Wait();

            // Assert
            scenario.End();
        }

        [Test]
        public void PingSendCorrectBytes()
        {
            // Setup
            var scenario = HidScenario.Build();
            BytesHolder holder = null;
            scenario.Write(report =>
            {
                var sizeHi = report.Data.Array[report.Data.Offset + 5];
                var sizeLo = report.Data.Array[report.Data.Offset + 6];
                var size = (sizeHi << 8) | sizeLo;
                holder = new BytesHolder(size);
                AssertWriteInitPacket(report.Data, TestChannel, U2FHidCommand.Ping, holder);
            });
            scenario.Read(() => new HidInputReport(BuildInitPacket(TestChannel, U2FHidCommand.Ping, holder)));

            var hid = CreateHidMock();
            var device = new U2FHidKey(hid.Object, false);
            InitDevice(hid, device);

            scenario.Run(hid);

            // Act
            device.Ping().Wait();

            // Assert
            scenario.End();
        }

        [Test]
        public void PingWithDataSendCorrectBytes()
        {
            // Setup
            var pingData = Encoding.UTF8.GetBytes("Testing !");
            var scenario = HidScenario.Build();
            scenario.Write(report => AssertWriteInitPacket(report.Data, TestChannel, U2FHidCommand.Ping, pingData));
            scenario.Read(() => new HidInputReport(BuildInitPacket(TestChannel, U2FHidCommand.Ping, pingData)));

            var hid = CreateHidMock();
            var device = new U2FHidKey(hid.Object, false);
            InitDevice(hid, device);

            scenario.Run(hid);

            // Act
            var returnedResponse = device.Ping(pingData.Segment()).Result;

            // Assert
            scenario.End();
            Check.That(returnedResponse.ContentEquals(pingData.Segment()));
        }

        [Test]
        public async Task PingWithContinuation()
        {
            // Setup
            var pingData = new byte[100];
            new Random().NextBytes(pingData);
            var writeInitData = pingData.Take(63 - 7).ToArray();
            var writeContData = pingData.Skip(63 - 7).Take(63 - 5).ToArray();

            var scenario = HidScenario.Build();
            scenario.Write(report => AssertWriteInitPacketSized(report.Data, TestChannel, U2FHidCommand.Ping, pingData.Length, writeInitData));
            scenario.Write(report => AssertWriteContinuation(report.Data, TestChannel, 0, writeContData));
            scenario.Read(() => new HidInputReport(BuildInitPacketSized(TestChannel, U2FHidCommand.Ping, pingData.Length, writeInitData)));
            scenario.Read(() => new HidInputReport(BuildContinuationPacket(TestChannel, 0, writeContData)));

            var hid = CreateHidMock();
            var device = new U2FHidKey(hid.Object, false);
            InitDevice(hid, device);

            scenario.Run(hid);

            // Act
            var returnedResponse = await device.Ping(pingData.Segment());

            // Assert
            scenario.End();
            Check.That(returnedResponse.ContentEquals(pingData.Segment()));
        }

        [Test]
        public void InitSendCorrectBytes()
        {
            // Setup
            var hid = CreateHidMock();
            var device = new U2FHidKey(hid.Object, false);

            // Act
            var init = InitDevice(hid, device);

            // Assert
            Check.That(init.Channel).IsEqualTo(0xCAFEBABE);
            Check.That(init.ProtocolVersion).IsEqualTo((byte)1);
            Check.That(init.Version).IsEqualTo(new Version(2,3,4));
            Check.That(init.Capabilities).IsEqualTo(U2FDeviceCapabilities.Wink);
        }

        const uint TestChannel = 0xCAFEBABE;
        const int PacketSize = 64;

        static U2FHidDeviceInfo InitDevice(Mock<IHidDevice> hid, U2FHidKey device)
        {
            // Setup
            var scenario = HidScenario.Build();
            var nonce = new BytesHolder(8);
            scenario.Write(report => AssertWriteInitPacket(report.Data, 0xffffffff, U2FHidCommand.Init, nonce));
            scenario.Read(() => new HidInputReport(BuildInitPacket(
                0xffffffff,
                U2FHidCommand.Init,
                nonce,
                (uint)TestChannel,
                (byte)1,
                (byte)2,
                (byte)3,
                (byte)4,
                (byte)U2FDeviceCapabilities.Wink)));

            scenario.Run(hid);
            var init = device.InitAsync().Result;
            scenario.End();
            return init;
        }

        static Task<T> ErrorTask<T>(Exception exception)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(exception);
            return tcs.Task;
        }

        static Mock<IHidDevice> CreateHidMock()
        {
            var hid = new Mock<IHidDevice>();
            hid.Setup(h => h.CreateOutputReport(It.IsAny<byte>()))
                .Returns<byte>(CreateOutputReport);
            return hid;
        }

        class TestHidOutputReport : IHidOutputReport
        {
            public byte Id { get; }
            public ArraySegment<byte> Data { get; }

            public TestHidOutputReport(byte id, ArraySegment<byte> data)
            {
                Id = id;
                Data = data;
            }
        }

        static IHidOutputReport CreateOutputReport(byte id)
        {
            return new TestHidOutputReport(id, new byte[PacketSize].Segment());
        }

        static void AssertWriteInitPacketSized(ArraySegment<byte> actual, uint channel, U2FHidCommand command, int size,
            params object[] data)
        {
            var sizeHi = (byte)((size >> 8) & 0xFF);
            var sizeLo = (byte)((size >> 0) & 0xFF);

            var expected = new object[] { channel, (byte)command, sizeHi, sizeLo }.Concat(data).ToArray();
            AssertBinary(actual, Endianness.BigEndian, expected);
        }

        static void AssertWriteInitPacket(ArraySegment<byte> actual, uint channel, U2FHidCommand command,
            params object[] data)
        {
            var size = GetSize(data);
            AssertWriteInitPacketSized(actual, channel, command, size, data);
        }

        private static ArraySegment<byte> BuildInitPacket(uint channel, U2FHidCommand command, params object[] data)
        {
            var size = GetSize(data);
            return BuildInitPacketSized(channel, command, size, data);
        }

        static ArraySegment<byte> BuildInitPacketSized(uint channel, U2FHidCommand command, int size, params object[] data)
        {
            var sizeHi = (byte)((size >> 8) & 0xFF);
            var sizeLo = (byte)((size >> 0) & 0xFF);

            var packet = new object[] { (byte)0, channel, (byte)command, sizeHi, sizeLo }.Concat(data).ToArray();
            return BuildBinary(Endianness.BigEndian, packet).Segment();
        }

        static void AssertWriteContinuation(ArraySegment<byte> actual, uint channel, byte sequence,
            params object[] data)
        {
            var expected = new object[] { channel, (byte)sequence }.Concat(data).ToArray();
            AssertBinary(actual, Endianness.BigEndian, expected);
        }

        static ArraySegment<byte> BuildContinuationPacket(uint channel, byte sequence, params object[] data)
        {
            var packet = new object[] { (byte)0, channel, (byte)sequence }.Concat(data).ToArray();
            var realContent = BuildBinary(Endianness.BigEndian, packet);
            var result = new byte[PacketSize];
            Array.Copy(realContent, result, realContent.Length);
            return result.Segment();
        }
    }
}
