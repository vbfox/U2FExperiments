using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Server.messages;
using JetBrains.Annotations;

namespace BlackFox.U2F.GnubbyApi
{
    public interface IU2FClient
    {
        [NotNull, ItemCanBeNull]
        Task<SignResponse> Sign([NotNull] ICollection<SignRequest> requests,
            CancellationToken cancellationToken = default(CancellationToken));

        [NotNull, ItemCanBeNull]
        Task<RegisterResponse> Register([NotNull] ICollection<RegisterRequest> registrationRequests,
            [NotNull] ICollection<SignRequest> signRequests, CancellationToken cancellationToken = default(CancellationToken));
    }
}