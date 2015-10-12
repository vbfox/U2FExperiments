using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlackFox.U2FHid.Core;
using BlackFox.U2FHid.Utils;
using BlackFox.UsbHid;
using Common.Logging;
using JetBrains.Annotations;

namespace BlackFox.U2FHid
{
    public class InitResponse
    {
        public uint Channel { get; }
        public byte ProtocolVersion { get; }
        public byte MajorVersionNumber { get; }
        public byte MinorVersionNumber { get; }
        public byte BuildVersionNumber { get; }
        public U2FDeviceCapabilities Capabilities { get; }

        public InitResponse(uint channel, byte protocolVersion, byte majorVersionNumber, byte minorVersionNumber,
            byte buildVersionNumber, U2FDeviceCapabilities capabilities)
        {
            Channel = channel;
            ProtocolVersion = protocolVersion;
            MajorVersionNumber = majorVersionNumber;
            MinorVersionNumber = minorVersionNumber;
            BuildVersionNumber = buildVersionNumber;
            Capabilities = capabilities;
        }
    }

    public class U2FDevice : IDisposable
    {
        static readonly ILog log = LogManager.GetLogger(typeof(FidoU2FHidPaketWriter));

        /// <summary>
        /// Size of the nonce for INIT messages in bytes.
        /// </summary>
        const int INIT_NONCE_SIZE = 8;
        const uint BROADCAST_CHANNEL = 0xffffffff;

        InitResponse initResponse;

        [NotNull]
        private readonly IHidDevice device;

        readonly bool closeDeviceOnDispose;

        public U2FDevice([NotNull] IHidDevice device, bool closeDeviceOnDispose)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            this.device = device;
            this.closeDeviceOnDispose = closeDeviceOnDispose;
        }

        uint GetChannel()
        {
            return initResponse?.Channel ?? BROADCAST_CHANNEL;
        }

        [NotNull]
        [ItemNotNull]
        public Task<ArraySegment<byte>> SendU2FMessage(ArraySegment<byte> message)
        {
            throw new NotImplementedException();
        }

        [NotNull]
        [ItemNotNull]
        public async Task<InitResponse> Init(ArraySegment<byte> nonce)
        {
            if (nonce.Count != INIT_NONCE_SIZE)
            {
                throw new ArgumentException(
                    $"Nonce should be exactly {INIT_NONCE_SIZE} bytes but is {nonce.Count} bytes long instead.",
                    nameof(nonce));
            }

            log.Info("Sending initialization");
            var message = new FidoU2FHidMessage(BROADCAST_CHANNEL, U2FHidCommand.Init, nonce);
            var answer = await Query(message);
            return OnInitAnswered(answer, nonce);
        }

        InitResponse OnInitAnswered(FidoU2FHidMessage response, ArraySegment<byte> nonce)
        {
            log.Info("Initialization response received");

            if (response.Data.Count < 17)
            {
                throw new Exception("Answer too small");
            }

            var nonceAnswer = response.Data.Segment(0, INIT_NONCE_SIZE);
            if (!nonceAnswer.ContentEquals(nonce))
            {
                throw new Exception("Invalid nonce, not an answer to our init");
            }

            var afterNonce = response.Data.Segment(INIT_NONCE_SIZE);
            using (var reader = new BinaryReader(afterNonce.AsStream()))
            {
                var channel = reader.ReadUInt32();
                var protocolVersion = reader.ReadByte();
                var majorVersionNumber = reader.ReadByte();
                var minorVersionNumber = reader.ReadByte();
                var buildVersionNumber = reader.ReadByte();
                var capabilities = (U2FDeviceCapabilities)reader.ReadByte();

                var parsedResponse = new InitResponse(channel, protocolVersion, majorVersionNumber, minorVersionNumber,
                    buildVersionNumber, capabilities);

                initResponse = parsedResponse;

                return parsedResponse;
            }
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

        public static async Task<U2FDevice> OpenAsync([NotNull] IHidDevice device, bool closeDeviceOnDispose = true)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            var instance = new U2FDevice(device, closeDeviceOnDispose);
            try
            {
                await instance.Init();
                return instance;
            }
            catch
            {
                instance.Dispose();
                throw;
            }
        }

        public Task Wink()
        {
            var message = new FidoU2FHidMessage(GetChannel(), U2FHidCommand.Wink);
            return Query(message);
        }

        public Task<bool> Lock()
        {
            throw new NotImplementedException();
        }

        public async Task<ArraySegment<byte>> Ping(ArraySegment<byte> pingData)
        {
            var message = new FidoU2FHidMessage(GetChannel(), U2FHidCommand.Ping, pingData);
            var response = await Query(message);
            if (!pingData.ContentEquals(response.Data))
            {
                throw new InvalidPingResponseException("The device didn't echo back our ping message.");
            }

            return response.Data;
        }

        async Task<FidoU2FHidMessage> Query(FidoU2FHidMessage query)
        {
            await device.WriteFidoU2FHidMessageAsync(query);
            var init = await device.ReadFidoU2FHidMessageAsync();

            if (init.Channel != query.Channel)
            {
                throw new Exception($"Bad channel in query answer (0x{init.Channel:X8} but expected 0x{query.Channel:X8})");
            }

            if (init.Command == U2FHidCommand.Error)
            {
                ThrowForError(init);
                return init;
            }

            if (init.Command != query.Command)
            {
                throw new Exception($"Bad command in query answer ({init.Command} but expected {query.Command})");
            }

            return init;
        }

        [ContractAnnotation("=> halt")]
        static void ThrowForError(FidoU2FHidMessage message)
        {
            Debug.Assert(message.Command == U2FHidCommand.Error);

            if (message.Data.Count < 1)
            {
                throw new Exception("Bad length for error");
            }

            var error = (U2FHidErrors)message.Data.AsEnumerable().First();

            throw new Exception("Error: " + EnumDescription.Get(error));
        }

        public async Task Ping()
        {
            var data = Encoding.UTF8.GetBytes("Pong !");
            await Ping(data.Segment());
        }

        public void Dispose()
        {
            if (closeDeviceOnDispose)
            {
                device.Dispose();
            }
        }
    }
}
