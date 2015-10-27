using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;
using Common.Logging;
using JetBrains.Annotations;

namespace BlackFox.U2F.GnubbyApi
{
    class RegisterOperation
    {
        static readonly TimeSpan timeBetweenRegisterCalls = TimeSpan.FromMilliseconds(200);
        static readonly TimeSpan timeBetweenOpenCalls = TimeSpan.FromMilliseconds(200);
        class KeyBusyException : Exception
        {

        }

        static readonly ILog log = LogManager.GetLogger(typeof(RegisterOperation));

        readonly IKeyId keyId;
        readonly ICollection<KeyRegisterRequest> requests;

        public RegisterOperation([NotNull] IKeyId keyId, [NotNull] ICollection<KeyRegisterRequest> requests)
        {
            if (keyId == null)
            {
                throw new ArgumentNullException(nameof(keyId));
            }
            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            this.keyId = keyId;
            this.requests = requests;
        }

        public async Task<RegisterOperationResult> AuthenticateAsync(CancellationToken cancellationToken)
        {
            using (var key = await OpenKeyAsync(cancellationToken))
            {
                while (true)
                {
                    var signOnceResult = await TryRegisteringOnceAsync(key, cancellationToken);

                    if (signOnceResult.HasValue)
                    {
                        return signOnceResult.Value;
                    }

                    await TaskEx.Delay(timeBetweenRegisterCalls, cancellationToken);
                }
            }
        }

        async Task<RegisterOperationResult?> TryRegisteringOnceAsync(IKey key, CancellationToken cancellationToken)
        {
            int challengesTried = 0;
            foreach (var request in requests)
            {
                challengesTried += 1;

                var requestSignResult = await TryOneRequest(key, request, cancellationToken);
                if (requestSignResult.HasValue)
                {
                    return requestSignResult.Value;
                }
            }
            if (challengesTried == 0)
            {
                return RegisterOperationResult.Failure(KeyResponseStatus.Failure);
            }

            return null;
        }

        async Task<RegisterOperationResult?> TryOneRequest(IKey key, KeyRegisterRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await key.RegisterAsync(request, cancellationToken);

                log.Info(result.Status.ToString());
                switch (result.Status)
                {
                    case KeyResponseStatus.Success:
                        return RegisterOperationResult.Success(request, result.Data);
                }
            }
            catch (KeyGoneException)
            {
                // No sense in continuing with this signer, the key isn't physically present anymore
                log.DebugFormat("Key '{0}' is gone", keyId);
                throw;
            }
            catch (TaskCanceledException)
            {
                // Let cancellation bubble up
                throw;
            }
            catch (KeyBusyException)
            {
                // Maybe it won't be busy later
            }
            catch (Exception exception)
            {
                log.Error("Authenticate request failed", exception);
            }
            return null;
        }

        private async Task<IKey> OpenKeyAsync(CancellationToken cancellationToken)
        {
            IKey key;
            do
            {
                try
                {
                    key = await keyId.OpenAsync(cancellationToken);
                }
                catch (KeyBusyException)
                {
                    key = null;
                    await TaskEx.Delay(timeBetweenOpenCalls, cancellationToken);
                }
            } while (key == null);
            return key;
        }
    }
}