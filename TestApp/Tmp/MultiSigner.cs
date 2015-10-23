using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Key.messages;
using JetBrains.Annotations;
using NLog;

namespace U2FExperiments.Tmp
{
    /// <summary>
    /// Continuously query all the keys connected for a signature, returning when either one key succeeded in signing
    /// or the request is cancelled.
    /// </summary>
    class MultiSigner
    {
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly TimeSpan timeBetweenKeyScansWithKey = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan timeBetweenKeyScansNoKeyDetected = TimeSpan.FromMilliseconds(300);
        private readonly IKeyFactory keyFactory;
        private readonly List<AuthenticateRequest> requests;

        public MultiSigner(IKeyFactory keyFactory, List<AuthenticateRequest> requests)
        {
            this.keyFactory = keyFactory;
            this.requests = requests;
        }

        readonly ConcurrentDictionary<IKeyId, bool> signersInProgress = new ConcurrentDictionary<IKeyId, bool>();

        [NotNull]
        [ItemCanBeNull]
        public async Task<SignerResult> SignAsync(CancellationToken cancellationToken)
        {
            signersInProgress.Clear();
            var childsCancellation = new CancellationTokenSource();
            var linkedChildCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                childsCancellation.Token);
            var tcs = new TaskCompletionSource<SignerResult>();
            var detection = NewSignersDetectionLoopAsync(tcs, linkedChildCancellation.Token);
            try
            {
                var finishedTask = await Task.WhenAny(tcs.Task, detection);
                if (finishedTask == tcs.Task)
                {
                    return tcs.Task.Result;
                }
                else
                {
                    return SignerResult.Failure(KeyResponseStatus.Failure);
                }
            }
            finally
            {
                // If we finish due to anything but a cancellation we need to cancel the running tasks
                childsCancellation.Cancel();
            }
        }

        private async Task NewSignersDetectionLoopAsync(TaskCompletionSource<SignerResult> tcs, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await AddNewSignersAsync(tcs, cancellationToken);
                var delay = signersInProgress.Count == 0 ? timeBetweenKeyScansNoKeyDetected : timeBetweenKeyScansWithKey;
                await Task.Delay(delay, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task AddNewSignersAsync(TaskCompletionSource<SignerResult> tcs, CancellationToken cancellationToken)
        {
            var allKeys = await keyFactory.FindAllAsync(cancellationToken);
            var newKeys = allKeys.Where(k => !signersInProgress.ContainsKey(k)).ToList();
            if (newKeys.Count > 0)
            {
                log.Info($"Found {newKeys.Count} new keys connected");

                foreach (var key in newKeys)
                {
                    signersInProgress.AddOrUpdate(key, k => StartSingleSigner(k, tcs, cancellationToken), (k, v) => v);
                }
            }
        }

        private bool StartSingleSigner(IKeyId keyId, TaskCompletionSource<SignerResult> tcs, CancellationToken cancellationToken)
        {
            var singleSigner = new SingleSigner(false, keyId, requests);
            var task = singleSigner.SignAsync(cancellationToken);
            task.ContinueWith(t =>
            {
                switch (t.Status)
                {
                    case TaskStatus.RanToCompletion:
                        if (t.Result.IsSuccess)
                        {
                            tcs.TrySetResult(t.Result);
                        }
                        // If the result is null it mean that there is no way that it can sign any of the requests,
                        // so we  remember it so that we won't query it again.
                        break;
                    case TaskStatus.Faulted:
                        // Will be re-discovered next iteration
                        ForgetSigner(keyId);
                        break;
                }
            }, cancellationToken);

            return true;
        }

        private void ForgetSigner(IKeyId keyId)
        {
            bool useless;
            signersInProgress.TryRemove(keyId, out useless);
        }
    }
}