using System;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Key.messages;

namespace BlackFox.U2F.Gnubby
{
    /// <summary>
    ///     Base implementation for a FIDO U2F device that communicate via APDU messages
    /// </summary>
    public abstract class U2FApduKey : IKey
    {
        protected const InteractionFlags EnforceUserPresenceAndSign =
            InteractionFlags.TestOfUserPresenceRequired | InteractionFlags.ConsumeTestOfUserPresence;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<KeyResponse<RegisterResponse>> RegisterAsync(RegisterRequest request,
            CancellationToken cancellationToken = new CancellationToken(),
            bool invididualAttestation = false)
        {
            var flags = invididualAttestation
                ? EnforceUserPresenceAndSign | InteractionFlags.AttestWithDeviceKey
                : EnforceUserPresenceAndSign;

            var message = ApduMessageCodec.EncodeRegisterRequest(new KeyRequest<RegisterRequest>(request, flags));
            var response = await QueryApduAsync(message, cancellationToken);
            return ApduMessageCodec.DecodeRegisterReponse(response);
        }

        public async Task<KeyResponse<AuthenticateResponse>> AuthenticateAsync(AuthenticateRequest request,
            CancellationToken cancellationToken = new CancellationToken(),
            bool noWink = false)
        {
            var flags = noWink
                ? EnforceUserPresenceAndSign | InteractionFlags.TestSignatureOnly
                : EnforceUserPresenceAndSign;

            var message = ApduMessageCodec.EncodeAuthenticateRequest(
                new KeyRequest<AuthenticateRequest>(request, flags));
            var response = await QueryApduAsync(message, cancellationToken);
            return ApduMessageCodec.DecodeAuthenticateReponse(response);
        }

        public async Task<KeyResponse<string>> GetVersionAsync(
            CancellationToken cancellationToken = new CancellationToken())
        {
            var message = ApduMessageCodec.EncodeVersionRequest();
            var response = await QueryApduAsync(message, cancellationToken);
            return ApduMessageCodec.DecodeVersionResponse(response);
        }

        public Task SyncAsync()
        {
            throw new NotImplementedException();
        }

        protected abstract void Dispose(bool disposing);

        protected abstract Task<ArraySegment<byte>> QueryAsync(ArraySegment<byte> request,
            CancellationToken cancellationToken = default(CancellationToken));

        public async Task<ApduResponse> QueryApduAsync(ApduRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var requestBytes = ApduCodec.EncodeRequest(request);
            var responseBytes = await QueryAsync(requestBytes, cancellationToken);
            return ApduCodec.DecodeResponse(responseBytes);
        }
    }
}
