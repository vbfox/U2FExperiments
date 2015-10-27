using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;

namespace BlackFox.U2F.GnubbyApi
{
    public struct SignerResult
    {
        public bool IsSuccess => Status == KeyResponseStatus.Success;
        public KeyResponseStatus Status { get; }
        public KeySignRequest Request { get; }
        public KeySignResponse Response { get; }

        private SignerResult(KeyResponseStatus status, KeySignRequest request, KeySignResponse response)
        {
            Status = status;
            Request = request;
            Response = response;
        }

        public static SignerResult Success(KeySignRequest request, KeySignResponse response)
        {
            return new SignerResult(KeyResponseStatus.Success, request, response);
        }

        public static SignerResult Failure(KeyResponseStatus status)
        {
            return  new SignerResult(status, null, null);
        }
    }
}