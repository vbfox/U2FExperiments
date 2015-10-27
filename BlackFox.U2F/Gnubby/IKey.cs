using System;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby.Messages;
using JetBrains.Annotations;

namespace BlackFox.U2F.Gnubby
{
    public interface IKey : IDisposable
    {
        [NotNull, ItemNotNull]
        Task<KeyResponse<KeyRegisterResponse>> RegisterAsync([NotNull] KeyRegisterRequest request,
            CancellationToken cancellationToken = default(CancellationToken), bool invididualAttestation = false);

        [NotNull, ItemNotNull]
        Task<KeyResponse<KeySignResponse>> SignAsync([NotNull] KeySignRequest request,
            CancellationToken cancellationToken = default(CancellationToken), bool noWink = false);

        [NotNull, ItemNotNull]
        Task<KeyResponse<string>> GetVersionAsync(CancellationToken cancellationToken = default(CancellationToken));

        //[NotNull]
        //Task WinkAsync(CancellationToken cancellationToken = default(CancellationToken));

        [NotNull]
        Task SyncAsync();
    }
}