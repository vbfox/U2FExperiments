using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;
using JetBrains.Annotations;

namespace BlackFox.U2F.GnubbyApi
{
    /// <summary>
    /// An implementation of <see cref="IKeyOperations"/> that uses all keys returned by a <see cref="IKeyFactory"/>.
    /// </summary>
    internal class KeyOperations : IKeyOperations
    {
        readonly IKeyFactory keyFactory;

        public KeyOperations([NotNull] IKeyFactory keyFactory)
        {
            if (keyFactory == null)
            {
                throw new ArgumentNullException(nameof(keyFactory));
            }

            this.keyFactory = keyFactory;
        }

        public Task<SignOperationResult> Sign(ICollection<KeySignRequest> requests, CancellationToken cancellationToken = new CancellationToken(),
            bool invididualAttestation = false)
        {
            var signer = new MultiKeyOperation<SignOperationResult>(
                keyFactory,
                (keyId, ct) => new SignOperation(keyId, requests).SignAsync(ct),
                r => r.IsSuccess
                );
            return signer.RunOperationAsync(cancellationToken);
        }

        public Task<RegisterOperationResult> Register(ICollection<KeyRegisterRequest> registerRequests, ICollection<KeySignRequest> signRequests,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var signer = new MultiKeyOperation<RegisterOperationResult>(
                keyFactory,
                (keyId, ct) => new RegisterOperation(keyId, registerRequests).AuthenticateAsync(ct),
                r => r.IsSuccess
                );
            return signer.RunOperationAsync(cancellationToken);
        }
    }
}