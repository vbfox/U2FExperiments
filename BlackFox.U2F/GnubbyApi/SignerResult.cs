using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Key.messages;

namespace BlackFox.U2F.GnubbyApi
{
    internal struct SignerResult
    {
        public bool IsSuccess => Status == KeyResponseStatus.Success;
        public KeyResponseStatus Status { get; }
        public AuthenticateRequest Request { get; }
        public AuthenticateResponse Response { get; }

        private SignerResult(KeyResponseStatus status, AuthenticateRequest request, AuthenticateResponse response)
        {
            Status = status;
            Request = request;
            Response = response;
        }

        public static SignerResult Success(AuthenticateRequest request, AuthenticateResponse response)
        {
            return new SignerResult(KeyResponseStatus.Success, request, response);
        }

        public static SignerResult Failure(KeyResponseStatus status)
        {
            return  new SignerResult(status, null, null);
        }
    }
}