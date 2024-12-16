namespace Ares.src.Utils.Extra
{
    public class FormatterUtil
    {
        public static string formatMillis(long ms)
        {
            return (TimeUtil.CurrentTimeMillis() - ms) + "ms";
        }

        public static string formatSeconds(long ms)
        {
            long seconds = ((TimeUtil.CurrentTimeMillis() - ms) / 1000);

            return (seconds > 0 ? seconds + "s" : formatMillis(ms));
        }
    }
}