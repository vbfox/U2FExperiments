using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby.Messages;
using JetBrains.Annotations;

namespace BlackFox.U2F.GnubbyApi
{
    /// <summary>
    /// Represent operations that can be run on one or multiple keys
    /// </summary>
    public interface IKeyOperations
    {
        [NotNull]
        Task<SignOperationResult> Sign([NotNull] ICollection<KeySignRequest> requests,
            CancellationToken cancellationToken = default(CancellationToken), bool invididualAttestation = false);

        [NotNull]
        Task<RegisterOperationResult> Register(ICollection<KeyRegisterRequest> registerRequests, ICollection<KeySignRequest> signRequests,
            CancellationToken cancellationToken = new CancellationToken());
    }
}