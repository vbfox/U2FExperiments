using System;
using System.Threading.Tasks;
using BlackFox.UsbHid.Portable;
using JetBrains.Annotations;

namespace BlackFox.U2FHid
{
    [Flags]
    enum FidoU2FDeviceCapabilities
    {
        Wink = 0x01
    }

    class InitResponse
    {
        public ArraySegment<byte> Nonce { get; }
        public uint Channel { get; }
        public byte ProtocolVersion { get; }
        public byte MajorVersionNumber { get; }
        public byte MinorVersionNumber { get; }
        public byte BuildVersionNumber { get; }
        public FidoU2FDeviceCapabilities Capabilities { get; }
    }

    class FidoU2FDevice
    {
        const int INIT_NONCE_SIZE = 8;

        InitResponse initResponse;

        public byte ProtocolVersion { get; }
        public byte MajorVersionNumber { get; }
        public byte MinorVersionNumber { get; }
        public byte BuildVersionNumber { get; }
        public FidoU2FDeviceCapabilities Capabilities { get; }

        [NotNull]
        private IHidDevice device;

        private FidoU2FDevice([NotNull] IHidDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            this.device = device;
        }

        uint GetChannel()
        {
            return initResponse?.Channel ?? 0;
        }

        [NotNull]
        [ItemNotNull]
        public Task<ArraySegment<byte>> SendU2FMessage(ArraySegment<byte> message)
        {
            throw new NotImplementedException();
        }

        [NotNull]
        [ItemNotNull]
        public Task<InitResponse> Init(ArraySegment<byte> nonce)
        {
            if (nonce.Count != INIT_NONCE_SIZE)
            {
                throw new ArgumentException(
                    $"Nonce should be exactly {INIT_NONCE_SIZE} bytes but is {nonce.Count} bytes long.",
                    nameof(nonce));
            }

            throw new NotImplementedException();
        }

        static readonly Random random = new Random();

        [NotNull]
        [ItemNotNull]
        public Task<InitResponse> Init()
        {
            var nonce = new byte[INIT_NONCE_SIZE];
            
            // No need for a cryptographic random as the value is only used to distinguish between multiple potential
            // parallel requests
            random.NextBytes(nonce);

            return Init(nonce.Segment());
        }

        public static Task<FidoU2FDevice> Open([NotNull] IHidDevice device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            var instance = new FidoU2FDevice(device);
            return instance.Init()
                .ContinueWith(
                    response => instance,
                    TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
