extern alias LoggingPcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F;
using BlackFox.U2F.Codec;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Key.messages;
using LoggingPcl::Common.Logging;
using ILog = Common.Logging.ILog;

namespace U2FExperiments.Tmp
{
    internal struct SignerResult
    {
        public bool IsSuccess { get; }
        public AuthenticateRequest Request { get; }
        public AuthenticateResponse Response { get; }

        private SignerResult(bool isSuccess, AuthenticateRequest request, AuthenticateResponse response)
        {
            IsSuccess = isSuccess;
            Request = request;
            Response = response;
        }

        public static SignerResult Success(AuthenticateRequest request, AuthenticateResponse response)
        {
            return new SignerResult(true, request, response);
        }

        public static SignerResult Failure()
        {
            return  new SignerResult(false, null, null);
        }
    }

    class SingleSigner
    {
        private static readonly TimeSpan timeBetweenChecks = TimeSpan.FromSeconds(1);
        class KeyBusyException:Exception
        {
             
        }
        static readonly ILog log = LogManager.GetLogger(typeof(SingleSigner));

        public bool ForEnroll { get; }
        public IKeyId KeyId { get; }
        public ICollection<AuthenticateRequest> Requests { get; }
        private readonly List<byte[]> blacklistedKeyHandles = new List<byte[]>();

        public SingleSigner(bool forEnroll, IKeyId keyId, ICollection<AuthenticateRequest> requests)
        {
            ForEnroll = forEnroll;
            KeyId = keyId;
            Requests = requests;
        }

        public async Task<SignerResult> Sign(CancellationToken cancellationToken)
        {
            using (var key = await OpenKeyAsync(cancellationToken))
            {
                var firstPass = true;
                while (true)
                {
                    int challengesTried = 0;
                    foreach (var request in Requests)
                    {
                        if (IsBlacklisted(request))
                        {
                            continue;
                        }

                        challengesTried += 1;
                        try
                        {
                            var result = await key.AuthenticateAsync(request, cancellationToken, firstPass);

                            log.Info(result.Status.ToString());
                            switch (result.Status)
                            {
                                case KeyResponseStatus.BadKeyHandle:
                                    // No use retrying
                                    blacklistedKeyHandles.Add(request.KeyHandle);
                                    break;

                                case KeyResponseStatus.Success:
                                    return SignerResult.Success(request, result.Data);
                            }
                        }
                        catch (KeyGoneException)
                        {
                            log.DebugFormat("Key '{0}' is gone", KeyId);
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
                    }
                    if (challengesTried == 0)
                    {
                        return SignerResult.Failure();
                    }

                    if (!firstPass)
                    {
                        await Task.Delay(timeBetweenChecks, cancellationToken);
                    }
                    firstPass = false;
                }
            }
        }

        private async Task<U2FVersion> GetVersionAsync(IKey key, CancellationToken cancellationToken)
        {
            do
            {
                try
                {
                    var response = await key.GetVersionAsync(cancellationToken);
                    if (response.Status == KeyResponseStatus.Success)
                    {
                        U2FVersion parsedVersion;
                        if (VersionCodec.TryDecodeVersion(response.Data, out parsedVersion))
                        {
                            return parsedVersion;
                        }
                    }
                    throw new InvalidOperationException("Unexpected answer to version query: " + response.Data);
                }
                catch (KeyBusyException)
                {
                    
                }
            } while (true);
        }

        private async Task<IKey> OpenKeyAsync(CancellationToken cancellationToken)
        {
            IKey key;
            do
            {
                try
                {
                    key = await KeyId.OpenAsync(cancellationToken);
                }
                catch (KeyBusyException)
                {
                    key = null;
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
