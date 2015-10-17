using System;

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
    }
}