using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using Common.Logging;
using JetBrains.Annotations;

namespace BlackFox.U2F.GnubbyApi
{
    /// <summary>
    /// Run an operation on all the keys, returning when either one key succeeded
    /// or the request is cancelled.
    /// </summary>
    internal class MultiKeyOperation<T>
    {
        private static readonly ILog log = LogManager.GetLogger(typeof (MultiKeyOperation<T>));

        private readonly IKeyFactory keyFactory;
        private readonly Func<IKeyId, CancellationToken, Task<T>> operation;
        private readonly Func<T, bool> resultValidator;

        private readonly Dictionary<IKeyId, bool> signersInProgress = new Dictionary<IKeyId, bool>();
        private readonly object signersInProgressLock = new object();
        private readonly TimeSpan timeBetweenKeyScansNoKeyDetected;
        private readonly TimeSpan timeBetweenKeyScansWithKey;

        public MultiKeyOperation(IKeyFactory keyFactory, Func<IKeyId, CancellationToken, Task<T>> operation,
            Func<T, bool> resultValidator, TimeSpan? timeBetweenKeyScansWithKey = null,
            TimeSpan? timeBetweenKeyScansNoKeyDetected = null)
        {
            this.keyFactory = keyFactory;
            this.operation = operation;
            this.resultValidator = resultValidator;
            this.timeBetweenKeyScansWithKey = timeBetweenKeyScansWithKey ?? defaultTimeBetweenKeyScansWithKey;
            this.timeBetweenKeyScansNoKeyDetected =
                timeBetweenKeyScansNoKeyDetected ?? defaultTimeBetweenKeyScansNoKeyDetected;
        }

        [NotNull]
        [ItemCanBeNull]
        public async Task<T> RunOperationAsync(CancellationToken cancellationToken)
        {
            signersInProgress.Clear();
            var childsCancellation = new CancellationTokenSource();
            var linkedChildCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                childsCancellation.Token);
            var tcs = new TaskCompletionSource<T>();
            var detection = NewSignersDetectionLoopAsync(tcs, linkedChildCancellation.Token);
            try
            {
                var finishedTask = await TaskEx.WhenAny(tcs.Task, detection);
                await finishedTask; // Throw if a cancellation or anything wrong hapenned
                Debug.Assert(finishedTask == tcs.Task,
                    "Only the completion source should finish normally, the other task can only finish in error");
                return tcs.Task.Result;
            }
            finally
            {
                // If we finish due to anything but a cancellation we need to cancel the running tasks
                childsCancellation.Cancel();
            }
        }

        private async Task NewSignersDetectionLoopAsync(TaskCompletionSource<T> tcs, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await AddNewSignersAsync(tcs, cancellationToken);
                var delay = signersInProgress.Count == 0 ? timeBetweenKeyScansNoKeyDetected : timeBetweenKeyScansWithKey;
                await TaskEx.Delay(delay, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task AddNewSignersAsync(TaskCompletionSource<T> tcs, CancellationToken cancellationToken)
        {
            Dictionary<IKeyId, bool> localSignersInProgress;
            lock (signersInProgressLock)
            {
                localSignersInProgress = new Dictionary<IKeyId, bool>(signersInProgress);
            }
            var allKeys = await keyFactory.FindAllAsync(cancellationToken);
            var newKeys = allKeys.Where(k => !localSignersInProgress.ContainsKey(k)).ToList();
            if (newKeys.Count > 0)
            {
                log.Info($"Found {newKeys.Count} new keys connected");

                lock (signersInProgressLock)
                {
                    foreach (var key in newKeys)
                    {
                        signersInProgress[key] = StartSingleSigner(key, tcs, cancellationToken);
                    }
                }
            }
        }

        private bool StartSingleSigner(IKeyId keyId, TaskCompletionSource<T> tcs, CancellationToken cancellationToken)
        {
            var task = operation(keyId, cancellationToken);
            task.ContinueWith(t =>
            {
                switch (t.Status)
                {
                    case TaskStatus.RanToCompletion:
                        if (resultValidator(t.Result))
                        {
                            bool setResultSucceeded = tcs.TrySetResult(t.Result);
                            Debug.Assert(setResultSucceeded);
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
            lock (signersInProgressLock)
            {
                signersInProgress.Remove(keyId);
            }
        }

        // ReSharper disable StaticMemberInGenericType
        private static readonly TimeSpan defaultTimeBetweenKeyScansWithKey = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan defaultTimeBetweenKeyScansNoKeyDetected = TimeSpan.FromMilliseconds(300);
        // ReSharper restore StaticMemberInGenericType
    }
}