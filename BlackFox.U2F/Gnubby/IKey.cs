using System;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Key.messages;
using JetBrains.Annotations;

namespace BlackFox.U2F.Gnubby
{
    public interface IKey : IDisposable
    {
        [NotNull, ItemNotNull]
        Task<KeyResponse<RegisterResponse>> RegisterAsync([NotNull] RegisterRequest request,
            CancellationToken cancellationToken = default(CancellationToken), bool invididualAttestation = false);

        [NotNull, ItemNotNull]
        Task<KeyResponse<AuthenticateResponse>> AuthenticateAsync([NotNull] AuthenticateRequest request,
            CancellationToken cancellationToken = default(CancellationToken), bool noWink = false);

        [NotNull, ItemNotNull]
        Task<KeyResponse<string>> GetVersionAsync(CancellationToken cancellationToken = default(CancellationToken));

        [NotNull]
        Task SyncAsync();
    }
}