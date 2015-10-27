using System.Text;
using BlackFox.Binary;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;

namespace BlackFox.U2F.Codec
{
    /// <summary>
    /// Encode and decode the messages exchanged with the key in APDU format
    /// </summary>
    public static class ApduMessageCodec
    {
        const byte RegisterCommand = 0x01;
        const byte AuthenticateCommand = 0x02;
        const byte VersionCommand = 0x03;

        public static ApduRequest EncodeRegisterRequest(KeyRequest<KeyRegisterRequest> request)
        {
            var requestBytes = RawMessageCodec.EncodeKeyRegisterRequest(request.Request);
            return new ApduRequest(RegisterCommand, (byte)request.Flags, 0x00, requestBytes.Segment());
        }

        public static KeyResponse<KeyRegisterResponse> DecodeRegisterReponse(ApduResponse apdu)
        {
            var status = ParseKeyResponseStatus(apdu.Status);
            var response = status == KeyResponseStatus.Success
                ? RawMessageCodec.DecodeKeyRegisterResponse(apdu.ResponseData)
                : null;

            return new KeyResponse<KeyRegisterResponse>(apdu, response, status);
        }

        public static ApduRequest EncodeAuthenticateRequest(KeyRequest<KeySignRequest> request)
        {
            var requestBytes = RawMessageCodec.EncodeKeySignRequest(request.Request);
            return new ApduRequest(AuthenticateCommand, (byte)request.Flags, 0x00, requestBytes.Segment());
        }

        public static ApduRequest EncodeVersionRequest()
        {
            return new ApduRequest(VersionCommand, 0x00, 0x00, EmptyArraySegment<byte>.Value);
        }

        public static KeyResponse<string> DecodeVersionResponse(ApduResponse apdu)
        {
            if (apdu.Status == ApduResponseStatus.InsNotSupported)
            {
                return new KeyResponse<string>(apdu, U2FConsts.U2Fv1, KeyResponseStatus.Success);
            }
            if (apdu.Status != ApduResponseStatus.NoError)
            {
                return new KeyResponse<string>(apdu, null, ParseKeyResponseStatus(apdu.Status));
            }

            // TODO: String is ASCII not UTF-8 we should gracefully handle characters with the high bit set
            var str = Encoding.UTF8.GetString(apdu.ResponseData.Array, apdu.ResponseData.Offset, apdu.ResponseData.Count);
            return new KeyResponse<string>(apdu, str, KeyResponseStatus.Success);
        }

        static KeyResponseStatus ParseKeyResponseStatus(ApduResponseStatus raw)
        {
            switch (raw)
            {
                case ApduResponseStatus.NoError:
                    return KeyResponseStatus.Success;

                case ApduResponseStatus.ConditionsNotSatisfied:
                    return KeyResponseStatus.TestOfuserPresenceRequired;

                case ApduResponseStatus.WrongData:
                    return KeyResponseStatus.BadKeyHandle;

                default:
                    return KeyResponseStatus.Failure;
            }
        }

        public static KeyResponse<KeySignResponse> DecodeAuthenticateReponse(ApduResponse apdu)
        {
            var status = ParseKeyResponseStatus(apdu.Status);
            var response = status == KeyResponseStatus.Success
                ? RawMessageCodec.DecodeKeySignResponse(apdu.ResponseData)
                : null;

            return new KeyResponse<KeySignResponse>(apdu, response, status);
        }
    }
}