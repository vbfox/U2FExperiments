extern alias LoggingPcl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    class SingleSigner : IDisposable
    {
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

        public async Task<AuthenticateResponse> Sign(CancellationToken cancellationToken)
        {
            using (var key = await OpenKeyAsync(cancellationToken))
            {
                var version = await GetVersionAsync(key, cancellationToken);
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
                            var result = await key.AuthenticateAsync(request, cancellationToken);
                            if (result.Status == KeyResponseStatus.BadKeyHandle)
                            {
                                // No use retrying
                                blacklistedKeyHandles.Add(request.KeyHandle);
                            }
                        }
                        catch (Exception exception)
                        {
                            log.Error("Authenticate request failed", exception);
                        }
                    }
                    if (challengesTried == 0)
                    {
                        return null;
                    }
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

        public void Dispose()
        {
            
        }
    }
}
