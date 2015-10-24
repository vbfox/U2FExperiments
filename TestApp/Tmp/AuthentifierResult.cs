using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Key.messages;

namespace U2FExperiments.Tmp
{
    internal struct AuthentifierResult
    {
        public bool IsSuccess => Status == KeyResponseStatus.Success;
        public KeyResponseStatus Status { get; }
        public RegisterRequest Request { get; }
        public RegisterResponse Response { get; }

        private AuthentifierResult(KeyResponseStatus status, RegisterRequest request, RegisterResponse response)
        {
            Status = status;
            Request = request;
            Response = response;
        }

        public static AuthentifierResult Success(RegisterRequest request, RegisterResponse response)
        {
            return new AuthentifierResult(KeyResponseStatus.Success, request, response);
        }

        public static AuthentifierResult Failure(KeyResponseStatus status)
        {
            return new AuthentifierResult(status, null, null);
        }
    }
}