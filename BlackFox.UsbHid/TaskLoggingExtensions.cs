using System;
using System.Threading.Tasks;
using Common.Logging;
using JetBrains.Annotations;

namespace BlackFox.UsbHid.Portable
{
    public static class TaskLoggingExtensions
    {
        [NotNull]
        public static Task LogFaulted([NotNull] this Task task, [NotNull] ILog log, [NotNull] string message,
            params object[] args)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (message == null) throw new ArgumentNullException(nameof(message));

            task.ContinueWith(faultedTask => log.ErrorFormat(message, faultedTask.Exception, args),
                TaskContinuationOptions.OnlyOnFaulted);

            return task;
        }

        [NotNull]
        public static Task<T> LogFaulted<T>([NotNull] this Task<T> task, [NotNull] ILog log, [NotNull] string message,
            params object[] args)
        {
            LogFaulted((Task)task, log, message, args);
            return task;
        }
    }
}
