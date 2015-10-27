using System;
using BlackFox.Binary;

namespace BlackFox.U2F.Codec
{
    /// <summary>
    /// An APDU (Smart card application protocol data unit) response message.
    /// </summary>
    public struct ApduResponse
    {
        public ApduResponseStatus Status { get; }

        public ArraySegment<byte> ResponseData { get; }

        public ApduResponse(ApduResponseStatus status, ArraySegment<byte> responseData)
        {
            Status = status;
            ResponseData = responseData;
        }

        public static ApduResponse Empty(ApduResponseStatus status)
        {
            return new ApduResponse(status, EmptyArraySegment<byte>.Value);
        }
    }
}