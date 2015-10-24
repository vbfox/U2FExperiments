using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Key.messages;
using Common.Logging;

namespace BlackFox.U2F.GnubbyApi
{
    class Signer
    {
        static readonly TimeSpan timeBetweenSignCalls = TimeSpan.FromMilliseconds(200);
        static readonly TimeSpan timeBetweenOpenCalls = TimeSpan.FromMilliseconds(200);

        class KeyBusyException:Exception
        {
             
        }
        static readonly ILog log = LogManager.GetLogger(typeof(Signer));

        readonly bool forEnroll;
        readonly IKeyId keyId;
        readonly ICollection<AuthenticateRequest> requests;
        private readonly List<byte[]> blacklistedKeyHandles = new List<byte[]>();

        public Signer(bool forEnroll, IKeyId keyId, ICollection<AuthenticateRequest> requests)
        {
            this.forEnroll = forEnroll;
            this.keyId = keyId;
            this.requests = requests;
        }

        public async Task<SignerResult> SignAsync(CancellationToken cancellationToken)
        {
            using (var key = await OpenKeyAsync(cancellationToken))
            {
                var firstPass = true;
                while (!forEnroll)
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

                return SignerResult.Failure(KeyResponseStatus.Failure);
            }
        }

        async Task<SignerResult?> TrySigningOnceAsync(IKey key, bool isFirstPass, CancellationToken cancellationToken)
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
                return SignerResult.Failure(KeyResponseStatus.Failure);
            }

            return null;
        }

        async Task<SignerResult?> TrySignOneRequest(IKey key, bool isFirstPass, AuthenticateRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await key.AuthenticateAsync(request, cancellationToken, isFirstPass || forEnroll);

                log.Info(result.Status.ToString());
                switch (result.Status)
                {
                    case KeyResponseStatus.BadKeyHandle:
                        // No use retrying
                        blacklistedKeyHandles.Add(request.KeyHandle);
                        break;

                    case KeyResponseStatus.Success:
                        return SignerResult.Success(request, result.Data);

                    case KeyResponseStatus.TestOfuserPresenceRequired:
                        if (forEnroll)
                        {
                            return SignerResult.Failure(result.Status);
                        }
                        break;
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

        private bool IsBlacklisted(AuthenticateRequest request)
        {
            return blacklistedKeyHandles.Any(h => h.SequenceEqual(request.KeyHandle));
        }
    }
}
