namespace Ares.Core.Util;

internal class TimeUtil
{
    public static long CurrentTimeMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}