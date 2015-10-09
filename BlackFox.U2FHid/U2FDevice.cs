using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using BlackFox.U2FHid.Core;
using BlackFox.U2FHid.Utils;
using BlackFox.UsbHid.Portable;
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
        public Task<InitResponse> Init(ArraySegment<byte> nonce)
        {
            if (nonce.Count != INIT_NONCE_SIZE)
            {
                throw new ArgumentException(
                    $"Nonce should be exactly {INIT_NONCE_SIZE} bytes but is {nonce.Count} bytes long instead.",
                    nameof(nonce));
            }

            var message = new FidoU2FHidMessage(BROADCAST_CHANNEL, U2FHidCommand.Init, nonce);
            return Query(message)
                .ContinueWith(
                    task => OnInitAnswered(task.Result, nonce),
                    TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        InitResponse OnInitAnswered(FidoU2FHidMessage response, ArraySegment<byte> nonce)
        {
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

        public static Task<U2FDevice> Open([NotNull] IHidDevice device, bool closeDeviceOnDispose = true)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            var instance = new U2FDevice(device, closeDeviceOnDispose);
            return instance.Init()
                .ContinueWith(
                    response => instance,
                    TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public Task Wink()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Lock()
        {
            throw new NotImplementedException();
        }

        public Task<ArraySegment<byte>> Ping(ArraySegment<byte> pingData)
        {
            var message = new FidoU2FHidMessage(GetChannel(), U2FHidCommand.Ping, pingData);
            return Query(message).ContinueWith(task => task.Result.Data);
        }

        Task<FidoU2FHidMessage> Query(FidoU2FHidMessage query)
        {
            return device.WriteFidoU2FHidMessageAsync(query)
                .ContinueWith(_ => device.ReadFidoU2FHidMessageAsync(), TaskContinuationOptions.OnlyOnRanToCompletion)
                .Unwrap()
                .ContinueWith(task =>
                {
                    if (task.Result.Channel != query.Channel)
                    {
                        throw new Exception("Woopsie");
                    }
                    if (task.Result.Command == query.Command)
                    {
                        return task.Result;
                    }
                    else if (task.Result.Command == U2FHidCommand.Error)
                    {
                        ThrowForError(task.Result);
                        return task.Result;
                    }
                    else
                    {
                        throw new Exception("Bad answer ???");
                    }
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        void ThrowForError(FidoU2FHidMessage message)
        {
            Debug.Assert(message.Command == U2FHidCommand.Error);

            if (message.Data.Count < 1)
            {
                throw new Exception("Bad length for error");
            }

            var error = (U2FHidErrors)message.Data.AsEnumerable().First();

            throw new Exception("Error: " + EnumDescription.Get(error));
        }

        public Task Ping()
        {
            const string ANSWER = "Pong !";

            var data = Encoding.UTF8.GetBytes(ANSWER);
            return Ping(data.Segment()).ContinueWith(task =>
            {
                var str = Encoding.UTF8.GetString(task.Result.Array,
                    task.Result.Offset, task.Result.Count);

                if (str != ANSWER)
                {
                    throw new InvalidPingResponseException("The device didn't echo back our ping message.");
                }

                return true;
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
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
