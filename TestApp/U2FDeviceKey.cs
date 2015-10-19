using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.Binary;
using BlackFox.U2F;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key;
using BlackFox.U2F.Key.messages;
using BlackFox.U2FHid;
using JetBrains.Annotations;

namespace U2FExperiments
{

    public enum AuthenticateMode
    {
        CheckOnly = 0x07,
        EnforseUserPresenceAndSign = 0x03
    }

    struct KeyRegisterRequest
    {
        public RegisterRequest Request { get; }

        public KeyRegisterRequest(RegisterRequest request)
        {
            Request = request;
        }
    }

    struct KeyAuthenticateRequest
    {
        public AuthenticateRequest Request { get; }
        public AuthenticateMode Mode { get; }

        public KeyAuthenticateRequest(AuthenticateRequest request, AuthenticateMode mode)
        {
            Request = request;
            Mode = mode;
        }
    }

    enum KeyRegisterReponseStatus
    {
        Success,
        TestOfuserPresenceRequired,
        Failure
    }

    struct KeyRegisterResponse
    {
        [CanBeNull]
        public RegisterResponse Data { get; }
        public KeyRegisterReponseStatus Status { get; }
        public ApduResponse Raw { get; }

        public KeyRegisterResponse(ApduResponse raw, [CanBeNull] RegisterResponse data, KeyRegisterReponseStatus status) : this()
        {
            Raw = raw;
            Status = status;
            Data = data;
        }
    }

    enum KeyAuthenticateResponseStatus
    {
        Success,
        TestOfuserPresenceRequired,
        BadKeyHandle,
        Failure
    }

    struct KeyAuthenticateResponse
    {
        [CanBeNull]
        public AuthenticateResponse Data { get; }
        public KeyAuthenticateResponseStatus Status { get; }
        public ApduResponse Raw { get; }

        public KeyAuthenticateResponse(ApduResponse raw, [CanBeNull] AuthenticateResponse data, KeyAuthenticateResponseStatus status) : this()
        {
            Raw = raw;
            Status = status;
            Data = data;
        }
    }

    static class KeyProtocolCodec
    {
        const byte RegisterCommand = 0x01;
        const byte AuthenticateCommand = 0x02;
        const byte VersionCommand = 0x03;

        public static ArraySegment<byte> EncodeRegisterRequest(KeyRegisterRequest request)
        {
            var requestBytes = RawMessageCodec.EncodeRegisterRequest(request.Request);
            var apdu = new ApduRequest(RegisterCommand, 0x00, 0x00, requestBytes.Segment());
            return ApduCodec.EncodeRequest(apdu);
        }

        static KeyRegisterReponseStatus ParseKeyRegisterReponseStatus(ApduResponseStatus raw)
        {
            switch (raw)
            {
                case ApduResponseStatus.NoError:
                    return KeyRegisterReponseStatus.Success;

                case ApduResponseStatus.ConditionsNotSatisfied:
                    return KeyRegisterReponseStatus.TestOfuserPresenceRequired;

                default:
                    return KeyRegisterReponseStatus.Failure;
            }
        }

        public static KeyRegisterResponse DecodeRegisterReponse(ArraySegment<byte> bytes)
        {
            var adpu = ApduCodec.DecodeResponse(bytes);

            var status = ParseKeyRegisterReponseStatus(adpu.Status);
            var response = status == KeyRegisterReponseStatus.Success
                ? RawMessageCodec.DecodeRegisterResponse(adpu.ResponseData)
                : null;

            return new KeyRegisterResponse(adpu, response, status);
        }

        public static ArraySegment<byte> EncodeAuthenticateRequest(KeyAuthenticateRequest request, U2FVersion version)
        {
            var requestBytes = RawMessageCodec.EncodeAuthenticateRequest(request.Request, version);
            var apdu = new ApduRequest(AuthenticateCommand, (byte)request.Mode, 0x00, requestBytes.Segment());
            return ApduCodec.EncodeRequest(apdu);
        }

        public static ArraySegment<byte> EncodeVersionRequest()
        {
            var apdu = new ApduRequest(VersionCommand, 0x00, 0x00, EmptyArraySegment<byte>.Value);
            return ApduCodec.EncodeRequest(apdu);
        }

        public static string DecodeVersionResponse(ArraySegment<byte> bytes)
        {
            var adpu = ApduCodec.DecodeResponse(bytes);
            if (adpu.Status == ApduResponseStatus.InsNotSupported)
            {
                return U2FConsts.U2Fv1;
            }
            return Encoding.ASCII.GetString(adpu.ResponseData.Array, adpu.ResponseData.Offset, adpu.ResponseData.Count);
        }

        static KeyAuthenticateResponseStatus ParseKeyAuthenticateReponseStatus(ApduResponseStatus raw)
        {
            switch (raw)
            {
                case ApduResponseStatus.NoError:
                    return KeyAuthenticateResponseStatus.Success;

                case ApduResponseStatus.ConditionsNotSatisfied:
                    return KeyAuthenticateResponseStatus.TestOfuserPresenceRequired;

                case ApduResponseStatus.WrongData:
                    return KeyAuthenticateResponseStatus.BadKeyHandle;

                default:
                    return KeyAuthenticateResponseStatus.Failure;
            }
        }

        public static KeyAuthenticateResponse DecodeAuthenticateReponse(ArraySegment<byte> bytes)
        {
            var adpu = ApduCodec.DecodeResponse(bytes);

            var status = ParseKeyAuthenticateReponseStatus(adpu.Status);
            var response = status == KeyAuthenticateResponseStatus.Success
                ? RawMessageCodec.DecodeAuthenticateResponse(adpu.ResponseData)
                : null;

            return new KeyAuthenticateResponse(adpu, response, status);
        }
    }

    class NiceDevice
    {
        private readonly U2FDevice device;

        public NiceDevice([NotNull] U2FDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            this.device = device;
        }

        public async Task<KeyRegisterResponse> RegisterAsync(RegisterRequest request)
        {
            var message = KeyProtocolCodec.EncodeRegisterRequest(new KeyRegisterRequest(request));
            var response = await device.SendU2FMessage(message);
            return KeyProtocolCodec.DecodeRegisterReponse(response);
        }

        public async Task<KeyAuthenticateResponse> AuthenticateAsync(AuthenticateRequest request, AuthenticateMode mode)
        {
            var version = await GetVersionAsync();
            var message = KeyProtocolCodec.EncodeAuthenticateRequest(new KeyAuthenticateRequest(request, mode), version);
            var response = await device.SendU2FMessage(message);
            return KeyProtocolCodec.DecodeAuthenticateReponse(response);
        }

        public async Task<U2FVersion> GetVersionAsync()
        {
            var version = await GetVersionStringAsync();
            return VersionCodec.DecodeVersion(version);
        }

        public async Task<string> GetVersionStringAsync()
        {
            var message = KeyProtocolCodec.EncodeVersionRequest();
            var response = await device.SendU2FMessage(message);
            return KeyProtocolCodec.DecodeVersionResponse(response);
        }
    }

    class U2FDeviceKey : IU2FKey
    {
        private readonly U2FDevice device;
        readonly NiceDevice niceDevice;

        public U2FDeviceKey([NotNull] U2FDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            this.device = device;
            niceDevice = new NiceDevice(device);
        }

        public RegisterResponse Register(RegisterRequest registerRequest)
        {
            while (true)
            {
                var result = niceDevice.RegisterAsync(registerRequest).Result;

                switch (result.Status)
                {
                    case KeyRegisterReponseStatus.Success:
                        Debug.Assert(result.Data != null, "no data for success");
                        return result.Data;
                    case KeyRegisterReponseStatus.TestOfuserPresenceRequired:
                        Console.WriteLine("User presence required");
                        Thread.Sleep(100);
                        continue;
                    case KeyRegisterReponseStatus.Failure:
                        throw new U2FException("Failure");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public AuthenticateResponse Authenticate(AuthenticateRequest authenticateRequest)
        {
            while (true)
            {
                var result = niceDevice.AuthenticateAsync(authenticateRequest, AuthenticateMode.EnforseUserPresenceAndSign).Result;

                switch (result.Status)
                {
                    case KeyAuthenticateResponseStatus.Success:
                        Debug.Assert(result.Data != null, "no data for success");
                        return result.Data;
                    case KeyAuthenticateResponseStatus.TestOfuserPresenceRequired:
                        Console.WriteLine("User presence required");
                        Thread.Sleep(100);
                        continue;
                    case KeyAuthenticateResponseStatus.Failure:
                        throw new U2FException("Failure: " + result.Raw.Status);
                    case KeyAuthenticateResponseStatus.BadKeyHandle:
                        throw new U2FException("Bad key handle");
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
