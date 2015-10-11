using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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
            scenario.Write(report => AssertWriteInitPacket(report.Data, TEST_CHANNEL, U2FHidCommand.Wink));
            scenario.Read(() => new HidInputReport(BuildInitPacket(TEST_CHANNEL, U2FHidCommand.Wink)));

            var hid = CreateHidMock();
            var device = new U2FDevice(hid.Object, false);
            InitDevice(hid, device);

            scenario.Run(hid);

            // Act
            device.Wink().Wait();

            // Assert
            scenario.End();
        }

        [Test]
        public void InitSendCorrectBytes()
        {
            // Setup
            var hid = CreateHidMock();
            var device = new U2FDevice(hid.Object, false);

            // Act
            var init = InitDevice(hid, device);

            // Assert
            Check.That(init).IsNotNull();
            Check.That(init.Channel).IsEqualTo(0xCAFEBABE);
            Check.That(init.ProtocolVersion).IsEqualTo((byte)1);
            Check.That(init.MajorVersionNumber).IsEqualTo((byte)2);
            Check.That(init.MinorVersionNumber).IsEqualTo((byte)3);
            Check.That(init.BuildVersionNumber).IsEqualTo((byte)4);
            Check.That(init.Capabilities).IsEqualTo(U2FDeviceCapabilities.Wink);
        }

        const uint TEST_CHANNEL = 0xCAFEBABE;

        static InitResponse InitDevice(Mock<IHidDevice> hid, U2FDevice device)
        {
            // Setup
            var scenario = HidScenario.Build();
            var nonce = new BytesHolder(8);
            scenario.Write(report => AssertWriteInitPacket(report.Data, 0xffffffff, U2FHidCommand.Init, nonce));
            scenario.Read(() => new HidInputReport(BuildInitPacket(
                0xffffffff,
                U2FHidCommand.Init,
                nonce,
                (uint)TEST_CHANNEL,
                (byte)1,
                (byte)2,
                (byte)3,
                (byte)4,
                (byte)U2FDeviceCapabilities.Wink)));

            scenario.Run(hid);
            var init = device.Init().Result;
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

        static HidOutputReport CreateOutputReport(byte id)
        {
            return new HidOutputReport(id, new byte[64].Segment());
        }

        static void AssertWriteInitPacket(ArraySegment<byte> actual, uint channel, U2FHidCommand command,
            params object[] data)
        {
            var size = GetSize(data);
            var sizeHi = (byte)((size >> 8) & 0xFF);
            var sizeLo = (byte)((size >> 0) & 0xFF);

            var expected = new object[] { channel, (byte)command, sizeHi, sizeLo }.Concat(data).ToArray();
            AssertBinary(actual, expected);
        }

        static ArraySegment<byte> BuildInitPacket(uint channel, U2FHidCommand command, params object[] data)
        {
            var size = GetSize(data);
            var sizeHi = (byte)((size >> 8) & 0xFF);
            var sizeLo = (byte)((size >> 0) & 0xFF);

            var packet = new object[] { (byte)0, channel, (byte)command, sizeHi, sizeLo }.Concat(data).ToArray();
            return BuildBinary(packet).Segment();
        }
    }
}
