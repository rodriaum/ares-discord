namespace Ares.src.Utils.Extra;

internal class TimeUtil
{
    public static long CurrentTimeMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}