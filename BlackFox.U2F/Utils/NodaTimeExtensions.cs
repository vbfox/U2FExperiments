using NodaTime;

namespace BlackFox.U2F.Utils
{
    static class NodaTimeExtensions
    {
        public static long ToUnixTimeMilliseconds(this Instant instant)
        {
            return instant.Ticks / NodaConstants.TicksPerMillisecond;
        }
    }
}
