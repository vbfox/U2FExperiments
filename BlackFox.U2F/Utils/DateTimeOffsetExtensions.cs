using System;

namespace BlackFox.U2F.Utils
{
    static class DateTimeOffsetExtensions
    {
        private static readonly long epochTicks;

        static DateTimeOffsetExtensions()
        {
            var time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            epochTicks = time.Ticks;
        }

        public static long ToMillisecondsSinceEpoch(this DateTimeOffset dateTimeOffset)
        {
            return (((dateTimeOffset.Ticks - dateTimeOffset.Offset.Ticks) - epochTicks) / TimeSpan.TicksPerMillisecond);
        }
    }
}
