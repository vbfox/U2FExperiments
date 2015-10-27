using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;

namespace BlackFox.U2F.GnubbyApi
{
    public struct AuthentifierResult
    {
        public bool IsSuccess => Status == KeyResponseStatus.Success;
        public KeyResponseStatus Status { get; }
        public KeyRegisterRequest Request { get; }
        public KeyRegisterResponse Response { get; }

        private AuthentifierResult(KeyResponseStatus status, KeyRegisterRequest request, KeyRegisterResponse response)
        {
            Status = status;
            Request = request;
            Response = response;
        }

        public static AuthentifierResult Success(KeyRegisterRequest request, KeyRegisterResponse response)
        {
            return new AuthentifierResult(KeyResponseStatus.Success, request, response);
        }

        public static AuthentifierResult Failure(KeyResponseStatus status)
        {
            return new AuthentifierResult(status, null, null);
        }
    }
}