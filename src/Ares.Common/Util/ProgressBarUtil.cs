namespace Ares.Common.Util;

public class ProgressBarUtil
{
    public static string GenerateProgressBar(int currentSecond, int totalSeconds, int barWidth = 30)
    {
        int filled = (currentSecond * barWidth) / totalSeconds;
        int empty = barWidth - filled;

        string filledBar = new string('█', filled);
        string emptyBar = new string('-', empty);

        return $"[{filledBar}{emptyBar}] {currentSecond}/{totalSeconds}s";
    }

    public static void DrawProgressBar(int currentSecond, int totalSeconds, ConsoleColor filledColor, bool useColor = true, int barWidth = 30)
    {
        int filled = (currentSecond * barWidth) / totalSeconds;
        int empty = barWidth - filled;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("[");

        if (useColor)
            Console.ForegroundColor = filledColor;
        Console.Write(new string('█', filled));

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('-', empty));

        Console.Write("] ");

        Console.ResetColor();
        Console.Write($"{currentSecond}s | {totalSeconds}");

        Console.SetCursorPosition(0, Console.CursorTop);
    }
}
