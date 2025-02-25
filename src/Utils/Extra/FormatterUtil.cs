namespace Ares.src.Utils.Extra;

public class FormatterUtil
{
    public static string FormatMillis(long ms)
    {
        return (TimeUtil.CurrentTimeMillis() - ms) + "ms";
    }

    public static string FormatSeconds(long ms)
    {
        long seconds = ((TimeUtil.CurrentTimeMillis() - ms) / 1000);

        return (seconds > 0 ? seconds + "s" : FormatMillis(ms));
    }
}