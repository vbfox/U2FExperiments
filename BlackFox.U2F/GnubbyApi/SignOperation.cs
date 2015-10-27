using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Gnubby.Messages;
using Common.Logging;

namespace BlackFox.U2F.GnubbyApi
{
    class SignOperation
    {
        static readonly TimeSpan timeBetweenSignCalls = TimeSpan.FromMilliseconds(200);
        static readonly TimeSpan timeBetweenOpenCalls = TimeSpan.FromMilliseconds(200);

        class KeyBusyException:Exception
        {
             
        }
        static readonly ILog log = LogManager.GetLogger(typeof(SignOperation));

        readonly IKeyId keyId;
        readonly ICollection<KeySignRequest> requests;
        private readonly List<byte[]> blacklistedKeyHandles = new List<byte[]>();

        public SignOperation(IKeyId keyId, ICollection<KeySignRequest> requests)
        {
            this.keyId = keyId;
            this.requests = requests;
        }

        public async Task<SignOperationResult> SignAsync(CancellationToken cancellationToken)
        {
            using (var key = await OpenKeyAsync(cancellationToken))
            {
                var firstPass = true;
                while (true)
                {
                    var signOnceResult = await TrySigningOnceAsync(key, firstPass, cancellationToken);

                    if (signOnceResult.HasValue)
                    {
                        return signOnceResult.Value;
                    }

                    if (!firstPass)
                    {
                        await TaskEx.Delay(timeBetweenSignCalls, cancellationToken);
                    }
                    firstPass = false;
                }
            }
        }

        async Task<SignOperationResult?> TrySigningOnceAsync(IKey key, bool isFirstPass, CancellationToken cancellationToken)
        {
            int challengesTried = 0;
            foreach (var request in requests)
            {
                if (IsBlacklisted(request))
                {
                    continue;
                }
                challengesTried += 1;

                var requestSignResult = await TrySignOneRequest(key, isFirstPass, request, cancellationToken);
                if (requestSignResult.HasValue)
                {
                    return requestSignResult.Value;
                }
            }
            if (challengesTried == 0)
            {
                return SignOperationResult.Failure(KeyResponseStatus.Failure);
            }

            return null;
        }

        async Task<SignOperationResult?> TrySignOneRequest(IKey key, bool isFirstPass, KeySignRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await key.SignAsync(request, cancellationToken, isFirstPass);

                log.Info(result.Status.ToString());
                switch (result.Status)
                {
                    case KeyResponseStatus.BadKeyHandle:
                        // No use retrying
                        blacklistedKeyHandles.Add(request.KeyHandle);
                        break;

                    case KeyResponseStatus.Success:
                        return SignOperationResult.Success(request, result.Data);
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

        private bool IsBlacklisted(KeySignRequest request)
        {
            return blacklistedKeyHandles.Any(h => h.SequenceEqual(request.KeyHandle));
        }
    }
}
