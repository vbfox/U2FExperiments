using System;
using BlackFox.U2F.Codec;
using JetBrains.Annotations;

namespace BlackFox.U2F.Gnubby
{
    public struct KeyResponse<TData>
        where TData : class
    {
        [CanBeNull]
        public TData Data { get; }

        public KeyResponseStatus Status { get; }
        public ApduResponse Raw { get; }

        public KeyResponse(ApduResponse raw, [CanBeNull] TData data, KeyResponseStatus status)
        {
            Raw = raw;
            Status = status;
            Data = data;
        }

        public KeyResponse<TFailed> ToFailedOf<TFailed>()
            where TFailed : class
        {
            if (Data != null)
            {
                throw new InvalidOperationException("The response isn't failed");
            }

            return new KeyResponse<TFailed>(Raw, null, Status);
        }

        public static KeyResponse<TData> Empty(ApduResponseStatus apduStatus, KeyResponseStatus status)
        {
            return new KeyResponse<TData>(ApduResponse.Empty(apduStatus), null, status);
        }
    }
}