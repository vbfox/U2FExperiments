using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.Binary;
using BlackFox.U2FHid.Core;
using BlackFox.U2FHid.Utils;
using BlackFox.UsbHid;
using Common.Logging;
using JetBrains.Annotations;
using static BlackFox.U2FHid.Core.U2FHidConsts;

namespace BlackFox.U2FHid
{
    public class U2FDevice : IDisposable
    {
        static readonly ILog log = LogManager.GetLogger(typeof(FidoU2FHidPaketWriter));

        public U2FDeviceInfo? DeviceInfo { get; private set; }

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
            return DeviceInfo?.Channel ?? BroadcastChannel;
        }

        [NotNull]
        [ItemNotNull]
        public async Task<ArraySegment<byte>> SendU2FMessage(ArraySegment<byte> message, CancellationToken cancellationToken = default(CancellationToken))
        {
            var fidoMessage = new FidoU2FHidMessage(GetChannel(), U2FHidCommand.Message, message);
            var response = await Query(fidoMessage, cancellationToken);
            return response.Data;
        }

        [NotNull]
        [ItemNotNull]
        public async Task<U2FDeviceInfo> Init(ArraySegment<byte> nonce, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (nonce.Count != InitNonceSize)
            {
                throw new ArgumentException(
                    $"Nonce should be exactly {InitNonceSize} bytes but is {nonce.Count} bytes long instead.",
                    nameof(nonce));
            }

            log.Info("Sending initialization");
            var message = new FidoU2FHidMessage(BroadcastChannel, U2FHidCommand.Init, nonce);
            var answer = await Query(message, cancellationToken);
            return OnInitAnswered(answer, nonce);
        }

        U2FDeviceInfo OnInitAnswered(FidoU2FHidMessage response, ArraySegment<byte> requestNonce)
        {
            log.Info("Initialization response received");

            ArraySegment<byte> responseNonce;
            var deviceInfo = MessageCodec.DecodeInitResponse(response.Data, out responseNonce);

            if (!responseNonce.ContentEquals(requestNonce))
            {
                throw new Exception("Invalid nonce, not an answer to our init request");
            }

            DeviceInfo = deviceInfo;
            return deviceInfo;
        }

        static readonly Random random = new Random();

        [NotNull]
        [ItemNotNull]
        public Task<U2FDeviceInfo> Init()
        {
            var nonce = new byte[InitNonceSize];
            
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

        public Task Wink(CancellationToken cancellationToken = default(CancellationToken))
        {
            var message = new FidoU2FHidMessage(GetChannel(), U2FHidCommand.Wink);
            return Query(message, cancellationToken);
        }

        public Task<bool> ReleaseLock()
        {
            return Lock(0);
        }

        /// <summary>
        /// The lock command places an exclusive lock for one channel to communicate with the device.
        /// As long as the lock is active, any other channel trying to send a message will fail.
        /// In order to prevent a stalling or crashing application to lock the device indefinitely, a lock time up to
        /// 10 seconds may be set. An application requiring a longer lock has to send repeating lock commands to
        /// maintain the lock.
        /// </summary>
        public async Task<bool> Lock(byte timeInSeconds, CancellationToken cancellationToken = default (CancellationToken))
        {
            if (timeInSeconds > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(timeInSeconds), timeInSeconds,
                    "Lock time must be between 0 and 10s");
            }

            var data = new [] { timeInSeconds }.Segment();
            var message = new FidoU2FHidMessage(GetChannel(), U2FHidCommand.Lock, data);
            var response = await Query(message, cancellationToken, false);
            if (response.Command == U2FHidCommand.Error)
            {
                var errorCode = GetError(response);
                if (errorCode == U2FHidErrors.Busy)
                {
                    return false;
                }

                ThrowForError(response);
            }

            return true;
        }

        async Task<FidoU2FHidMessage> Query(FidoU2FHidMessage query, CancellationToken cancellationToken, bool throwErrors = true)
        {
            await device.WriteFidoU2FHidMessageAsync(query, cancellationToken);
            var init = await device.ReadFidoU2FHidMessageAsync(cancellationToken);

            if (init.Channel != query.Channel)
            {
                throw new Exception($"Bad channel in query answer (0x{init.Channel:X8} but expected 0x{query.Channel:X8})");
            }

            if (init.Command == U2FHidCommand.Error)
            {
                if (throwErrors)
                {
                    ThrowForError(init);
                }
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
            var error = GetError(message);
            throw new Exception("Error: " + EnumDescription.Get(error));
        }

        private static U2FHidErrors GetError(FidoU2FHidMessage message)
        {
            Debug.Assert(message.Command == U2FHidCommand.Error);

            if (message.Data.Count < 1)
            {
                throw new Exception("Bad length for error");
            }

            var error = (U2FHidErrors) message.Data.AsEnumerable().First();
            return error;
        }

        /// <summary>
        /// Sends a transaction to the device, which immediately echoes the same data back.
        /// This command is defined to be an uniform function for debugging, latency and performance measurements.
        /// </summary>
        public async Task Ping()
        {
            var data = Encoding.UTF8.GetBytes("Pong !");
            await Ping(data.Segment());
        }

        /// <summary>
        /// Sends a transaction to the device, which immediately echoes the same data back.
        /// This command is defined to be an uniform function for debugging, latency and performance measurements.
        /// </summary>
        public async Task<ArraySegment<byte>> Ping(ArraySegment<byte> pingData, CancellationToken cancellationToken = default(CancellationToken))
        {
            var message = new FidoU2FHidMessage(GetChannel(), U2FHidCommand.Ping, pingData);
            var response = await Query(message, cancellationToken);
            if (!pingData.ContentEquals(response.Data))
            {
                throw new InvalidPingResponseException("The device didn't echo back our ping message.");
            }

            return response.Data;
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
