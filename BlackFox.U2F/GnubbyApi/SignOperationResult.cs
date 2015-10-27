using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;

namespace BlackFox.U2F.GnubbyApi
{
    public struct SignOperationResult
    {
        public bool IsSuccess => Status == KeyResponseStatus.Success;
        public KeyResponseStatus Status { get; }
        public KeySignRequest Request { get; }
        public KeySignResponse Response { get; }

        private SignOperationResult(KeyResponseStatus status, KeySignRequest request, KeySignResponse response)
        {
            Status = status;
            Request = request;
            Response = response;
        }

        public static SignOperationResult Success(KeySignRequest request, KeySignResponse response)
        {
            return new SignOperationResult(KeyResponseStatus.Success, request, response);
        }

        public static SignOperationResult Failure(KeyResponseStatus status)
        {
            return  new SignOperationResult(status, null, null);
        }
    }
}