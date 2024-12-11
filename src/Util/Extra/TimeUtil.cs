namespace Ares.src.Util.Extra
{
    internal class TimeUtil
    {
        public static long CurrentTimeMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}