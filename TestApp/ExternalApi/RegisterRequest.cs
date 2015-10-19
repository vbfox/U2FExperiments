using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
}
