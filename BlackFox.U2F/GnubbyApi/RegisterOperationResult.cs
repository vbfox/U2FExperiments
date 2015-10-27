using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;

namespace BlackFox.U2F.GnubbyApi
{
    public struct RegisterOperationResult
    {
        public bool IsSuccess => Status == KeyResponseStatus.Success;
        public KeyResponseStatus Status { get; }
        public KeyRegisterRequest Request { get; }
        public KeyRegisterResponse Response { get; }

        private RegisterOperationResult(KeyResponseStatus status, KeyRegisterRequest request, KeyRegisterResponse response)
        {
            Status = status;
            Request = request;
            Response = response;
        }

        public static RegisterOperationResult Success(KeyRegisterRequest request, KeyRegisterResponse response)
        {
            return new RegisterOperationResult(KeyResponseStatus.Success, request, response);
        }

        public static RegisterOperationResult Failure(KeyResponseStatus status)
        {
            return new RegisterOperationResult(status, null, null);
        }
    }
}