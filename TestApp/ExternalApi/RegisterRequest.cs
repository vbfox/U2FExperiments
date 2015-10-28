using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlackFox.U2F;
using JetBrains.Annotations;

namespace U2FExperiments.ExternalApi
{
    class RegisterRequest
    {
        public U2FVersion Version { get; }
        public ArraySegment<byte> Challenge { get; }
        public string ApplicationId { get; }
    }

    internal class RegisterResponse
    {
        public ArraySegment<byte> RegistrationData { get; }
        public ArraySegment<byte> ClientData { get; }
    }

    internal class SignRequest
    {
        public U2FVersion Version { get; }
        public ArraySegment<byte> Challenge { get; }
        public ArraySegment<byte> KeyHandle { get; }
        public string ApplicationId { get; }
    }

    internal class SignResponse
    {
        public ArraySegment<byte> KeyHandle { get; }
        public ArraySegment<byte> SignatureData { get; }
        public ArraySegment<byte> ClientData { get; }


    }

    internal interface IGnubbyApi
    {
        [NotNull, ItemNotNull]
        Task<RegisterResponse> RegisterAsync(
            [NotNull, ItemNotNull] ICollection<RegisterRequest> registerRequests,
            [NotNull, ItemNotNull] ICollection<SignRequest> signRequests,
            TimeSpan? timeout = null);

        [NotNull, ItemNotNull]
        Task<SignResponse> SignAsync(
            [NotNull, ItemNotNull] ICollection<SignRequest> signRequests,
            TimeSpan? timeout = null);
    }

    internal class GnubbyApi: IGnubbyApi
    {
        public Task<RegisterResponse> RegisterAsync(ICollection<RegisterRequest> registerRequests, ICollection<SignRequest> signRequests, TimeSpan? timeout = null)
        {
            // 1. Get All devices present
            // 2. Ask all of them to sign each of the signRequests
            // 3. 
            throw new NotImplementedException();
        }

        public Task<SignResponse> SignAsync(ICollection<SignRequest> signRequests, TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }
    }
}
